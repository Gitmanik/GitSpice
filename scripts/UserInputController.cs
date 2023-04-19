using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public partial class UserInputController : Control
{
	public static UserInputController Instance;

	public CircuitManager Circuit = new CircuitManager();

	private ElementPort CurrentConnecting;
	private Line2D CurrentWire;

	private PackedScene ElementScene;
	private Node RootGUINode;

    public override void _Ready()
	{
		ElementScene = GD.Load<PackedScene>("scenes/resistor.tscn");
		RootGUINode = GetNode("/root/main/ElementContainer");

		Instance = this;
	}

    public override void _Input(InputEvent @event)
    {
		if (@event is InputEventKey key && key.Keycode == Key.Backspace)
		{
			GD.Print(Circuit.SerializeCircuitToJson());
			GetViewport().SetInputAsHandled();
			return;
		}

		if (CurrentWire != null && @event is InputEventMouseButton mouseClicked && mouseClicked.ButtonIndex == MouseButton.Right)
		{
			NLog.LogManager.GetCurrentClassLogger().Info("test");
			ResetConnecting();
			GetViewport().SetInputAsHandled();
			return;
		}
		if (CurrentWire != null && @event is InputEventMouseMotion mouseMoved)
		{
			CurrentWire.Points = new Vector2[] { CurrentConnecting.OffsetPosition, mouseMoved.GlobalPosition};
		}
    }

    private void ResetConnecting()
    {
		CurrentConnecting = null;
		CurrentWire = null;
    }

    public void ConnectClicked(ElementPort clickedPort)
	{	
		if (CurrentConnecting != null)
		{
			if (Circuit.ConnectionExists(CurrentConnecting.Data.Id, clickedPort.Data.Id))
			{
				CurrentWire.QueueFree();
				ResetConnecting();
				return;
			}
			var conn = Circuit.ConnectPorts(CurrentConnecting.Data.Id, clickedPort.Data.Id);
			CurrentWire.Points = new Vector2[] { CurrentConnecting.OffsetPosition, clickedPort.OffsetPosition};

			Circuit.BindConnection(conn, CurrentConnecting, clickedPort, CurrentWire);
			ResetConnecting();
		}
		else
		{
			CurrentConnecting = clickedPort;
			CurrentWire = new Line2D();
			CurrentWire.Points = new Vector2[] { CurrentConnecting.OffsetPosition + clickedPort.GlobalPosition};
			RootGUINode.AddChild(CurrentWire);
		}
	}


    public override void _UnhandledInput(InputEvent @event)
    {

		if (@event is InputEventMouseButton e && e.ButtonIndex == MouseButton.Left && e.Pressed)
		{
			GD.Print("Creating new Element");
			
			//TODO: Move to Element
			var eldata = new ElementData("Resistor");
			eldata.Data = new Dictionary<string, string>();
			eldata.Data.Add("testkey", "testval");

			eldata.Ports = new List<PortData>();
			eldata.Ports.Add(new PortData());
			eldata.Ports.Add(new PortData());
			//

			var newElement = ElementScene.Instantiate<Control>();
			Circuit.CreateElement(eldata);
			Circuit.BindElement(eldata, newElement as Element);

			newElement.Position = e.Position;
			RootGUINode.AddChild(newElement);
		}
    }

    public void MoveElement(Element element, InputEventMouseMotion e)
    {
		element.Position = e.Position;

		foreach (ElementPort port in element.Ports)
		{
			List<CircuitManager.BoundConnection> connections = Circuit.FindBoundConnections(port.Data.Id);
			foreach (var connection in connections)
			{
				connection.Line.Points = new Vector2 []{connection.Port1.OffsetPosition, connection.Port2.OffsetPosition};
			}	
		}

		GetViewport().SetInputAsHandled();
    }
}
