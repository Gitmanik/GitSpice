using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Element : Control
{
	private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
	public ElementData Data;
	public List<ElementPort> Ports;

	bool Moving = false;

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
	}
}
