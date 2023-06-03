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
            Ports[idx].Data = Data.Ports[idx];
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
                var loop = CircuitManager.Instance.CalculateLoop(Data.Ports[0].Id, Data.Ports[1].Id);

                CircuitManager.Instance.ColorLoop(loop, Moving ? GodotHelpers.RandomColor() : Colors.White);

                //TODO: Remove me
                if (Moving)
                {
                    string eq = CircuitManager.Instance.Calculate2ndKirchoffLaw(loop);
                    string res = CircuitManager.Instance.SolveLinearSystem(new List<string>() { eq }, this);
                    Logger.Info(res);
                    infoPanelText += $"[b]2nd Kirchoff:[/b] {eq}\n";
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

    public string GetVoltageSymbol()
    {
        switch (Data.Type)
        {
            case "Voltage_Source":
                return "";
            case "Resistor":
                return $"Ur{Data.Id}";
            case "Pole":
                return "";
            case "Current_Source":
                return "";
            default:
                throw new NotImplementedException();
        }
    }

    public string GetVoltage()
    {
        switch (Data.Type)
        {
            case "Voltage_Source":
                return Data.Data["Amount"];
            case "Resistor":
                return GetVoltageSymbol();
            case "Pole":
                return "0";
            case "Current_Source":
                return "0";
            default:
                throw new NotImplementedException();
        }

    }
}