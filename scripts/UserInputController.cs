using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public partial class UserInputController : Control
{
	public static UserInputController Instance;

	private ElementPort CurrentWiring;

	private Line2D CurrentWire;

	record conninfo
	{
		public Line2D conn;
		public ElementPort port1, port2;
	}

	private List<conninfo> conns = new List<conninfo>();

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
		GD.Print(JsonConvert.SerializeObject(port));

		if (CurrentWiring != null)
		{
			CurrentWire.Points = new Vector2[] { CurrentWiring.OffsetPosition, port.OffsetPosition};

			conns.Add(new conninfo{port1 = CurrentWiring, port2 = port, conn = CurrentWire});

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

    public void MoveElement(Element element, InputEventMouseMotion e)
    {
		GD.Print(JsonConvert.SerializeObject(element));
		element.Position = e.Position;

		List<conninfo> connss = conns.FindAll(x => element.Ports.Contains(x.port1) || element.Ports.Contains(x.port2));
		foreach (conninfo conn in connss)
		{
			// GD.Print(conn);
			conn.conn.Points = new Vector2 []{conn.port1.OffsetPosition, conn.port2.OffsetPosition};
		}

		GetViewport().SetInputAsHandled();
    }
}
