using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Element : Control
{
	public ElementData Data;
	public List<ElementPort> Ports;

	bool moving = false;

    public override void _Ready()
	{
		Ports = GetNode("Ports").GetChildren().ToList().Cast<ElementPort>().ToList();
		GD.Print(JsonConvert.SerializeObject(Data));		

		if (Data == null)
			throw new ArgumentException("Data is null");

		if (Data.Ports == null)
			throw new ArgumentException("Data.Ports is null");
		for (int idx = 0; idx < Data.Ports.Count; idx++)
		{
			Ports[idx].Data = Data.Ports[idx];
		}

	}

    public void _TextureRectGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseClick && mouseClick.ButtonIndex == MouseButton.Left && mouseClick.Pressed)
		{
			moving = !moving;
			GD.Print($"{Name} {moving}");
			GetViewport().SetInputAsHandled();
		}
    }

	public override void _Input(InputEvent @event)
	{
		if (moving && @event is InputEventMouseMotion mouseMove)
		{
			UserInputController.Instance.MoveElement(this, mouseMove);
		}
	}
}
