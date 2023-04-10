using Godot;
using System;

public partial class UserInputController : Control
{
	private PackedScene ElementScene;
	private Node RootGUINode;

    public override void _Ready()
	{
		GD.Print("gitspice");
		ElementScene = GD.Load<PackedScene>("scenes/element.tscn");
		RootGUINode = GetNode("/root/GUI/ElementContainer");
	}

    public override void _Input(InputEvent @event)
    {
		if (@event is InputEventMouseButton e && e.ButtonIndex == MouseButton.Left && e.Pressed)
		{
			var newElement = ElementScene.Instantiate<Control>();
			newElement.Position = e.Position;
			RootGUINode.AddChild(newElement);
		}
    }
}
