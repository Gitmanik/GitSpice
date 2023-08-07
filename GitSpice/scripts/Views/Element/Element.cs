using Gitmanik.Models;
using Gitmanik.Controllers;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public partial class Element : Control
{
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public ElementData Data = null;
    public List<ElementPort> Ports;

    /// <summary>
    /// True if this element is currently moving
    /// </summary>
    /// <value></value>
    public bool Selected;

    /// <summary>
    /// Indicates whether any Element is moving
    /// </summary>
    public static bool IsCurrentlyMoving = false;

    public override void _Ready()
    {
        Ports = GetNode("Ports").GetChildren().ToList().Cast<ElementPort>().ToList();

        if (Data == null)
            throw new ArgumentException("Data is null");

        if (Data.Ports == null)
            throw new ArgumentException("Data.Ports is null");
        for (int idx = 0; idx < Data.Ports.Count; idx++)
        {
            Ports[idx].PortId = Data.Ports[idx];
            Ports[idx].ParentElement = this;
        }

        Name = Data.Id;
    }

    const int IdFontSize = 14;
    Vector2 IdPosition = new Vector2(0, IdFontSize / 2);
    public override void _Draw()
    {
        // macOS DrawString hang fix
        // TODO: Make them labels
        if (AppController.Settings.Data.DebugDrawIds && OS.GetName() != "macOS")
            DrawString(ThemeDB.FallbackFont, IdPosition, Data.Id, HorizontalAlignment.Center, fontSize: IdFontSize);

        // the line starting and ending points
        Vector2 startingPoint = Ports[0].Position + new Vector2(16, 25);
        Vector2 endingPoint = Ports[1].Position + new Vector2(0, 25);
        DrawLine(startingPoint, endingPoint, Colors.Cyan, 2);

        // the arrow size and flatness
        float arrowSize = 10f;
        float flatness = 0.5f;

        // calculate the direction vector
        Vector2 direction = (endingPoint - startingPoint).Normalized();

        // calculate the side vectors
        Vector2 side1 = new Vector2(-direction.Y, direction.X);
        Vector2 side2 = new Vector2(direction.Y, -direction.X);

        // calculate the T-junction points
        Vector2 e1 = endingPoint + side1 * arrowSize * flatness;
        Vector2 e2 = endingPoint + side2 * arrowSize * flatness;

        // calculate the arrow edges
        Vector2 p1 = e1 - direction * arrowSize;
        Vector2 p2 = e2 - direction * arrowSize;

        // draw the arrow sides as a polygon
        DrawPolygon(new Vector2[] { endingPoint, p1, p2 }, new[] { Colors.Cyan });
    }

    public void _TextureRectGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseClick && mouseClick.ButtonIndex == MouseButton.Left && mouseClick.Pressed)
        {
            Selected = !Selected;
            IsCurrentlyMoving = Selected;
            Logger.Debug($"Selected: {Name} {Selected}");

            UserInputController.Instance.ElementSelected(this, Selected);
            GetViewport().SetInputAsHandled();
            return;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Selected && @event is InputEventMouseMotion mouseMove)
        {
            UserInputController.Instance.MoveElement(this, mouseMove);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (Selected && @event is InputEventMouseButton mouseButton && mouseButton.IsPressed())
        {
            if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                CircuitManager.Instance.RemoveElement(this);
                GetViewport().SetInputAsHandled();
                return;
            }

            if (ElementProvider.Instance.GetElementDefinition(Data.Type).AllowRotation)
            {
                if (mouseButton.ButtonIndex == MouseButton.WheelUp)
                {
                    CircuitManager.Instance.RotateElement(this, Rotation + AppController.Settings.Data.ElementRotationAmount);
                    GetViewport().SetInputAsHandled();
                    return;
                }
                if (mouseButton.ButtonIndex == MouseButton.WheelDown)
                {
                    CircuitManager.Instance.RotateElement(this, Rotation - AppController.Settings.Data.ElementRotationAmount);
                    GetViewport().SetInputAsHandled();
                    return;
                }
            }
        }
    }
}