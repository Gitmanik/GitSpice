using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

[JsonObject(MemberSerialization.OptIn)]
public partial class Element : Control
{
	[JsonProperty]
	public List<ElementPort> Ports;

	[JsonProperty]
    public string Guid;

	bool moving = false;

    public override void _Ready()
	{
		Ports = GetNode("Ports").GetChildren().ToList().Cast<ElementPort>().ToList();
	}

	public void LoadData()
	{
		
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
