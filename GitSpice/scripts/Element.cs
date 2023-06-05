using Gitmanik.Models;
using Gitmanik.Controllers;
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

    /// <summary>
    /// True if this element is currently moving
    /// </summary>
    /// <value></value>
    public bool Moving;

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

    public override void _Draw()
    {
        if (SettingsController.Instance.Data.DebugDrawIds)
            DrawString(ThemeDB.FallbackFont, Vector2.Zero, Data.Id, HorizontalAlignment.Center);
    }

    public void _TextureRectGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseClick && mouseClick.ButtonIndex == MouseButton.Left && mouseClick.Pressed)
        {
            Moving = !Moving;
            IsCurrentlyMoving = Moving;
            Logger.Debug($"Moving: {Name} {Moving}");

            string infoPanelText = "";

            if (Data.Ports.Count == 2)
            {
                var loop = CircuitManager.Instance.CalculateLoop(Data.Ports[0], Data.Ports[1]);

                CircuitManager.Instance.ColorLoop(loop, Moving ? GodotHelpers.RandomColor() : Colors.White);

                //TODO: Remove me
                if (Moving)
                {
                    string secondKirchhoff = CircuitManager.Instance.Calculate2ndKirchhoffLaw(loop);

                    var givens = CircuitManager.Instance.GetAllGivens(loop);
                    var givens_eq = new List<string>();
                    foreach (var kvp in givens)
                        givens_eq.Add($"{kvp.Key}={kvp.Value}");
                    givens_eq.Add(secondKirchhoff);

                    string res = CircuitManager.Instance.SolveLinearSystem(givens_eq, givens.Keys.ToList(), this);
                    Logger.Info(res);
                    infoPanelText += $"[b]2nd Kirchoff:[/b] {secondKirchhoff}\n";
                    infoPanelText += $"[b]Voltage value:[/b] {res}\n";
                    infoPanelText += string.Join('\n', Data.Data.ToList().ConvertAll(x => $"[b]{x.Key}:[/b] {x.Value}"));
                }
            }

            UserInputController.Instance.InfoPanel.Text = infoPanelText;

            GetViewport().SetInputAsHandled();
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
                    CircuitManager.Instance.RotateElement(this, Rotation + SettingsController.Instance.Data.ElementRotationAmount);
                    GetViewport().SetInputAsHandled();
                    return;
                }
                if (mouseButton.ButtonIndex == MouseButton.WheelDown)
                {
                    CircuitManager.Instance.RotateElement(this, Rotation - SettingsController.Instance.Data.ElementRotationAmount);
                    GetViewport().SetInputAsHandled();
                    return;
                }
            }
        }
    }
}