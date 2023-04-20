using Godot;
using Newtonsoft.Json;
using System.Collections.Generic;

public partial class UserInputController : Control
{
    private const string CircuitSavePath = "user://circuit.json";
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static UserInputController Instance;

    public CircuitManager CircuitManager = new CircuitManager();

    private ElementPort CurrentConnecting;
    private Line2D CurrentWire;

    private PackedScene ElementScene;
    private Node RootGUINode;

    public override void _EnterTree()
    {
        LogManager.ConfigNLog();
    }

    public override void _Ready()
    {
        ElementScene = GD.Load<PackedScene>("scenes/resistor.tscn");
        RootGUINode = GetNode("/root/main/ElementContainer");

        Instance = this;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key)
        {
            if (key.Keycode == Key.Backspace)
            {
                Logger.Warn(CircuitManager.SerializeCircuitToJson());
                GetViewport().SetInputAsHandled();
                return;
            }
            if (key.Keycode == Key.S)
            {
                SaveCircuit();
                return;
            }
            if (key.Keycode == Key.L)
            {
                LoadCircuit();
                return;
            }
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
            CurrentWire.Points = new Vector2[] { CurrentConnecting.OffsetPosition, mouseMoved.GlobalPosition };
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton e && e.ButtonIndex == MouseButton.Left && e.Pressed)
        {
            Logger.Debug("Creating new Element");

            //TODO: Move to Element
            var eldata = new ElementData("Resistor");
            eldata.Data = new Dictionary<string, string>();
            eldata.Data.Add("testkey", "testval");

            eldata.Ports = new List<PortData>();
            eldata.Ports.Add(new PortData());
            eldata.Ports.Add(new PortData());
            //

            var newElement = ElementScene.Instantiate<Control>() as Element;
            CircuitManager.CreateElement(eldata);
            CircuitManager.BindElement(eldata, newElement);

            newElement.Position = e.Position;
            newElement.Data.Position.X = e.Position.X;
            newElement.Data.Position.Y = e.Position.Y;

            RootGUINode.AddChild(newElement);
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
            if (CircuitManager.Circuit.ConnectionExists(CurrentConnecting.Data.Id, clickedPort.Data.Id))
            {
                CurrentWire.QueueFree();
                ResetConnecting();
                return;
            }
            var conn = CircuitManager.Circuit.ConnectPorts(CurrentConnecting.Data.Id, clickedPort.Data.Id);
            CurrentWire.Points = new Vector2[] { CurrentConnecting.OffsetPosition, clickedPort.OffsetPosition };

            CircuitManager.BindConnection(conn, CurrentConnecting, clickedPort, CurrentWire);
            ResetConnecting();
        }
        else
        {
            CurrentConnecting = clickedPort;
            CurrentWire = new Line2D();
            CurrentWire.Points = new Vector2[] { CurrentConnecting.OffsetPosition };
            RootGUINode.AddChild(CurrentWire);
        }
    }


    public void MoveElement(Element element, InputEventMouseMotion e)
    {
        element.Position = e.Position;
        element.Data.Position.X = e.Position.X;
        element.Data.Position.Y = e.Position.Y;

        foreach (ElementPort port in element.Ports)
        {
            List<CircuitManager.BoundConnection> connections = CircuitManager.FindBoundConnections(port.Data.Id);
            foreach (var connection in connections)
            {
                connection.Line.Points = new Vector2[] { connection.Port1.OffsetPosition, connection.Port2.OffsetPosition };
            }
        }

        GetViewport().SetInputAsHandled();
    }

    // TODO: Move to other Manager
    public void LoadCircuit()
    {
        foreach (Node n in RootGUINode.GetChildren())
        {
            n.QueueFree();
        }

        var file = FileAccess.Open(CircuitSavePath, FileAccess.ModeFlags.Read);
        var data = file.GetAsText();
        file.Close();
        Logger.Info(data);
        var circuit = JsonConvert.DeserializeObject<CircuitData>(data);

        CircuitManager.LoadCircuit(circuit);

        //TODO: Move logic to CircuitManager.LoadCircuit
        foreach (var eldata in CircuitManager.Circuit.Elements)
        {
            var newElement = ElementScene.Instantiate<Control>() as Element;
            newElement.Data = eldata;
            CircuitManager.BindElement(eldata, newElement);

            newElement.Position = new Vector2(eldata.Position.X, eldata.Position.Y);

            RootGUINode.AddChild(newElement);
        }

        foreach (var conn in CircuitManager.Circuit.Connections)
        {
            var port1 = CircuitManager.FindElementPort(conn.Port1);
            var port2 = CircuitManager.FindElementPort(conn.Port2);

            var line = new Line2D();
            line.Points = new Vector2[] { port1.OffsetPosition, port2.OffsetPosition };
            RootGUINode.AddChild(line);
            CircuitManager.BindConnection(conn, port1, port2, line);
        }
    }

    public void SaveCircuit()
    {
        Logger.Info("Saving current Circuit state");
        var file = FileAccess.Open(CircuitSavePath, FileAccess.ModeFlags.Write);
        var data = CircuitManager.SerializeCircuitToJson();
        Logger.Info(data);
        file.StoreString(data);
        file.Close();
    }
}
