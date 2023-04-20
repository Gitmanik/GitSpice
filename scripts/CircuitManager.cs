using System;
using System.Collections.Generic;
using Godot;

public partial class CircuitManager : Node
{
    /// <summary>
    /// Represents relation between ConnectionData and visual representations.
    /// </summary>
    public record BoundConnection
    {
        public ConnectionData Data;
        public ElementPort Port1;
        public ElementPort Port2;
        public Line2D Line;
    }

    /// <summary>
    /// Represents relation between ElementData and its visual representation.
    /// </summary>
    public record BoundElement
    {
        public ElementData Data;
        public Element Element;
    }

    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static CircuitManager Instance;

    public CircuitData Circuit = new CircuitData();

    private List<BoundElement> BoundElements = new List<BoundElement>();
    private List<BoundConnection> BoundConnections = new List<BoundConnection>();

    public static readonly string ElementContainerPath = "/root/main/ElementContainer";
    private Node ElementContainerScene;

    public override void _Ready()
    {
        Logger.Info("CircuitManager starting");
        ElementContainerScene = GetNode(ElementContainerPath);

        Instance = this;
    }

    #region Binding
    private List<BoundConnection> FindBoundConnections(string Port) => BoundConnections.FindAll(x => x.Data.IsConnected(Port));
    private void BindConnection(ConnectionData conn, ElementPort Port1, ElementPort Port2, Line2D line)
    {
        BoundConnections.Add(new BoundConnection { Data = conn, Port1 = Port1, Port2 = Port2, Line = line });
    }

    private void BindElement(ElementData eldata, Element element)
    {
        BoundElements.Add(new BoundElement { Data = eldata, Element = element });
        element.Data = eldata;
    }

    #endregion

    public ElementPort FindElementPort(string Port)
    {
        foreach (var element in BoundElements)
        {
            var port = element.Element.Ports.Find(x => x.Data.Id == Port);
            if (port != null)
                return port;
        }
        return null;
    }

    /// <summary>
    /// Created an element in CircuitData and its visual representation.
    /// Creates Element object (based on Type) and adds it to scene.
    /// Throws ArgumentException when element already exists.
    /// </summary>
    /// <param name="data">Element data</param>
    /// <returns>Element object</returns>
    public Element CreateElement(ElementData data)
    {
        if (Circuit.Elements.Contains(data))
        {
            throw new ArgumentException($"Tried to create already existing element! (ElementData: {data})");
        }
        Circuit.Elements.Add(data);

        var elementDef = ElementProvider.Instance.GetElement(data.Type);
        var elementScene = elementDef.Scene.Instantiate<Element>();
        BindElement(data, elementScene);

        ElementContainerScene.AddChild(elementScene);

        return elementScene;
    }

    /// <summary>
    /// Makes a connection in CircuitData and its visual representation. 
    /// Created Line2D and adds it to ElementContainer scene.
    /// Throws ArgumentException when connection already exists.
    /// </summary>
    /// <param name="Port1">Port</param>
    /// <param name="Port2">Port</param>
    /// <returns>Connection object</returns>
    public ConnectionData ConnectPorts(ElementPort Port1, ElementPort Port2)
    {
        ConnectionData existingConnection = FindConnection(Port1.Data.Id, Port2.Data.Id);
        if (existingConnection != null)
        {
            throw new ArgumentException($"Tried to create already existing connection! (Port1: {Port1}, Port2: {Port2})");
        }

        ConnectionData conn = new ConnectionData(Port1.Data.Id, Port2.Data.Id);
        Circuit.Connections.Add(conn);

        var line = new Line2D();
        line.Points = new Vector2[] { Port1.OffsetPosition, Port2.OffsetPosition };

        BindConnection(conn, Port1, Port2, line);

        ElementContainerScene.AddChild(line);

        return conn;
    }

    /// <summary>
    /// Finds ConnectionData object between ports
    /// </summary>
    /// <param name="Port1">Port</param>
    /// <param name="Port2">Port</param>
    /// <returns>ConnectionData object</returns>
    public ConnectionData FindConnection(string Port1, string Port2) => Circuit.Connections.Find(x => x.IsConnected(Port1) && x.IsConnected(Port2));

    /// <summary>
    /// Checks if Connection between ports exists.
    /// </summary>
    /// <param name="Port1">Port</param>
    /// <param name="Port2">Port</param>
    /// <returns>True if connection exists, otherwise false</returns>
    public bool ConnectionExists(string Port1, string Port2) => FindConnection(Port1, Port2) != null;

    /// <summary>
    /// Moves Element to given position
    /// </summary>
    /// <param name="element">Element to move</param>
    /// <param name="v">Godot Vector2 position</param>
    public void MoveElement(Element element, Godot.Vector2 v)
    {
        element.Position = v;
        element.Data.Position.X = v.X;
        element.Data.Position.Y = v.Y;

        //Update connections
        foreach (ElementPort port in element.Ports)
        {
            List<CircuitManager.BoundConnection> connections = FindBoundConnections(port.Data.Id);
            foreach (var connection in connections)
            {
                connection.Line.Points = new Vector2[] { connection.Port1.OffsetPosition, connection.Port2.OffsetPosition };
            }
        }
    }

    public void LoadCircuit(CircuitData circuit)
    {
        Logger.Debug("Loading Circuit");

        Circuit = circuit;
        BoundConnections.Clear();
        BoundElements.Clear();

        foreach (Node child in ElementContainerScene.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var eldata in Circuit.Elements)
        {
            var elementDef = ElementProvider.Instance.GetElement(eldata.Type);
            var newElement = elementDef.Scene.Instantiate<Control>() as Element;

            newElement.Data = eldata;
            newElement.Position = new Vector2(eldata.Position.X, eldata.Position.Y);

            BindElement(eldata, newElement);

            ElementContainerScene.AddChild(newElement);
        }

        foreach (var conn in Circuit.Connections)
        {
            var port1 = FindElementPort(conn.Port1);
            var port2 = FindElementPort(conn.Port2);

            var line = new Line2D();
            line.Points = new Vector2[] { port1.OffsetPosition, port2.OffsetPosition };

            BindConnection(conn, port1, port2, line);

            ElementContainerScene.AddChild(line);
        }
    }
}