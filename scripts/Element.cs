using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Element : Control
{
	private List<ElementPort> Ports;

	bool moving = false;

	public override void _Ready()
	{
		Ports = GetNode("Ports").GetChildren().ToList().Cast<ElementPort>().ToList();
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
			Position = mouseMove.Position;
			GetViewport().SetInputAsHandled();
		}
	}
}
