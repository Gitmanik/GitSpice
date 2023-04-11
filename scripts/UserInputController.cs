using Godot;
using System;

public partial class UserInputController : Control
{
	public static UserInputController Instance;

	private ElementPort CurrentWiring;

	private Line2D CurrentWire;

	private PackedScene ElementScene;
	private Node RootGUINode;

    public override void _Ready()
	{
		GD.Print("gitspice");
		ElementScene = GD.Load<PackedScene>("scenes/resistor.tscn");
		RootGUINode = GetNode("/root/main/ElementContainer");

		Instance = this;
	}

    public override void _Input(InputEvent @event)
    {
		if (CurrentWire != null && @event is InputEventMouseMotion e)
		{
			CurrentWire.Points = new Vector2[] { CurrentWiring.OffsetPosition, e.GlobalPosition};
		}
    }

	public void ConnectClicked(ElementPort port)
	{	
		if (CurrentWiring != null)
		{
			CurrentWire.Points = new Vector2[] { CurrentWiring.OffsetPosition, port.OffsetPosition};

			CurrentWiring = null;
			CurrentWire = null;
		}
		else
		{
			CurrentWiring = port;
			CurrentWire = new Line2D();
			CurrentWire.Points = new Vector2[] { CurrentWiring.OffsetPosition + port.GlobalPosition};
			RootGUINode.AddChild(CurrentWire);
		}
	}


    public override void _UnhandledInput(InputEvent @event)
    {
		if (@event is InputEventMouseButton e && e.ButtonIndex == MouseButton.Left && e.Pressed)
		{
			GD.Print("creating new");
			var newElement = ElementScene.Instantiate<Control>();
			newElement.Position = e.Position;
			RootGUINode.AddChild(newElement);
		}
    }
}
