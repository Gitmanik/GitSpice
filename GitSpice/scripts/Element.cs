using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public partial class Element : Control
{
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public ElementData Data;
    public List<ElementPort> Ports;

    //TODO: Private set
    public bool Moving = false;

    // TODO: Expose as configurable variable
    private const float RotationAmount = 0.33f;

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

    public void _TextureRectGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseClick && mouseClick.ButtonIndex == MouseButton.Left && mouseClick.Pressed)
        {
            Moving = !Moving;
            Logger.Debug($"Moving: {Name} {Moving}");
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Moving && @event is InputEventMouseMotion mouseMove)
        {
            UserInputController.Instance.MoveElement(this, mouseMove);
        }

        if (Moving && @event is InputEventMouseButton mouseButton && mouseButton.IsPressed() && ElementProvider.Instance.GetElementDefinition(Data.Type).AllowRotation)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                CircuitManager.Instance.RotateElement(this, Rotation + RotationAmount);
            }
            if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                CircuitManager.Instance.RotateElement(this, Rotation - RotationAmount);
            }
        }

    }
}
