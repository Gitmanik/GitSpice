using Gitmanik.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public partial class Element : Control
{
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public ElementData Data = null;
    public List<ElementPort> Ports;

    //TODO: Private set
    public bool Moving = false;

    /// <summary>
    /// Indicates whether any Element is moving
    /// </summary>
    public static bool IsCurrentlyMoving = false;

    // TODO: Expose as configurable variable
    private const float RotationAmount = 2 * Mathf.Pi / 8f;

    public override void _Ready()
    {
        Ports = GetNode("Ports").GetChildren().ToList().Cast<ElementPort>().ToList();

        if (Data == null)
            throw new ArgumentException("Data is null");

        if (Data.Ports == null)
            throw new ArgumentException("Data.Ports is null");
        for (int idx = 0; idx < Data.Ports.Count; idx++)
        {
            Ports[idx].Data = Data.Ports[idx];
            Ports[idx].ParentElement = this;
        }

        Name = Data.Id;
    }

    public override void _Draw()
    {
        // TODO: This should be togglable in config
        DrawString(ThemeDB.FallbackFont, Vector2.Zero, Data.Id, HorizontalAlignment.Center);
    }

    public void _TextureRectGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseClick && mouseClick.ButtonIndex == MouseButton.Left && mouseClick.Pressed)
        {
            Moving = !Moving;
            IsCurrentlyMoving = Moving;
            Logger.Debug($"Moving: {Name} {Moving}");

            if (Data.Ports.Count == 2)
            {
                ColorLoop(Moving);
            }
            GetViewport().SetInputAsHandled();
        }
    }

    private void ColorLoop(bool reset)
    {
        Color c = reset ? Colors.White : GodotHelpers.RandomColor();
        var loop = CircuitManager.Instance.CalculateLoop(Data.Ports[0].Id, Data.Ports[1].Id);
        for (int idx = 0; idx < loop.Count - 1; idx++)
        {
            var port1 = loop[idx];
            var port2 = loop[idx + 1];
            Logger.Debug($"Ports: {port1} {port2}");
            if (CircuitManager.Instance.GetElements().Any(e => e.Data.Ports.ConvertAll(p => p.Id).All(new List<string>() { port1, port2 }.Contains)))
            {
                Logger.Trace("Skipping element");
                continue;
            }
            var boundConnection = CircuitManager.Instance.FindBoundConnection(port1, port2);
            if (boundConnection == null)
            {
                Logger.Error($"Could not find bound connection for {port1} -> {port2}!");
                continue;
            }
            boundConnection.Line.DefaultColor = c;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Moving && @event is InputEventMouseMotion mouseMove)
        {
            UserInputController.Instance.MoveElement(this, mouseMove);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (Moving && @event is InputEventMouseButton mouseButton && mouseButton.IsPressed())
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
                    CircuitManager.Instance.RotateElement(this, Rotation + RotationAmount);
                    GetViewport().SetInputAsHandled();
                    return;
                }
                if (mouseButton.ButtonIndex == MouseButton.WheelDown)
                {
                    CircuitManager.Instance.RotateElement(this, Rotation - RotationAmount);
                    GetViewport().SetInputAsHandled();
                    return;
                }
            }
        }
    }
}
