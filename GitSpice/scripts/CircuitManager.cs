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

    private CircuitData Circuit = new CircuitData();

    private List<BoundElement> BoundElements = new List<BoundElement>();
    private List<BoundConnection> BoundConnections = new List<BoundConnection>();

    public static readonly string ElementContainerPath = "/root/main/ElementContainer";
    private Control ElementContainerScene;

    public override void _Ready()
    {
        Logger.Info("CircuitManager starting");
        ElementContainerScene = GetNode<Control>(ElementContainerPath);

        Instance = this;
    }

    #region Binding

    /// <summary>
    /// Creates a copy of BoundConnetions
    /// </summary>
    /// <typeparam name="BoundConnection"></typeparam>
    /// <returns>Copy of BoundConnections list</returns>
    public List<BoundConnection> GetBoundConnections() => new List<BoundConnection>(BoundConnections);

    /// <summary>
    /// Finds all Bound connections related to given Port Id
    /// </summary>
    /// <param name="Port">Port Id</param>
    /// <returns>List of BoundConnection</returns>
    public List<BoundConnection> FindBoundConnections(string Port) => BoundConnections.FindAll(x => x.Data.IsConnected(Port));

    /// <summary>
    /// Creates bound connection for ports. Ports should be unique
    /// </summary>
    /// <param name="conn">Connection to bind</param>
    /// <param name="Port1">Port to bind</param>
    /// <param name="Port2">Port to bind</param>
    /// <param name="line"></param>
    private void CreateBoundConnection(ConnectionData conn, ElementPort Port1, ElementPort Port2, Line2D line)
    {
        if (Port1.Data.Id == Port2.Data.Id)
        {
            Logger.Warn("Cannot connect port to itself!");
            return;
        }

        // TODO: Check if BoundConnection already exists

        BoundConnections.Add(new BoundConnection { Data = conn, Port1 = Port1, Port2 = Port2, Line = line });
    }

    /// <summary>
    /// Creates bound connection for element
    /// </summary>
    /// <param name="eldata">Element data</param>
    /// <param name="element">Scene element</param>
    private void CreateBoundElement(ElementData eldata, Element element)
    {
        if (BoundElements.Find(x => (x.Element == element || x.Data == eldata)) != null)
        {
            Logger.Warn("Tried to create BoundElement which already exists!");
            return;
        }
        BoundElements.Add(new BoundElement { Data = eldata, Element = element });
        element.Data = eldata;
    }

    #endregion

    /// <summary>
    /// Finds ElementPort object for given Port Id
    /// </summary>
    /// <param name="Port">Port Id</param>
    /// <returns>ElementPort scene object</returns>
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

        var elementDef = ElementProvider.Instance.GetElementDefinition(data.Type);
        var elementScene = elementDef.Scene.Instantiate<Element>();
        CreateBoundElement(data, elementScene);

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

        var line = CreateLine2D(new Vector2[] { Port1.Centroid, Port2.Centroid });

        CreateBoundConnection(conn, Port1, Port2, line);

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

        UpdateConnections(element);
    }

    /// <summary>
    /// Rotates element in scene and in Circuit data
    /// </summary>
    /// <param name="element"></param>
    /// <param name="rotation"></param>
    internal void RotateElement(Element element, float rotation)
    {
        element.Rotation = rotation;
        element.Data.Rotation = rotation;

        UpdateConnections(element);
    }

    /// <summary>
    /// Updates Line2Ds connected to all ports of given element
    /// </summary>
    /// <param name="element">Element to update</param>
    private void UpdateConnections(Element element)
    {
        //Update connections
        foreach (ElementPort port in element.Ports)
        {
            List<CircuitManager.BoundConnection> connections = FindBoundConnections(port.Data.Id);
            foreach (var connection in connections)
            {
                connection.Line.Points = new Vector2[] { connection.Port1.Centroid, connection.Port2.Centroid };
            }
        }
    }

    /// <summary>
    /// Saves all data to CircuitData and serializes it to JSON
    /// </summary>
    /// <returns>JSON of CircuitData</returns>
    public string SaveCircuit()
    {
        Circuit.UserPosition = ElementContainerScene.Position.ToNumerics();
        return Newtonsoft.Json.JsonConvert.SerializeObject(CircuitManager.Instance.Circuit);
    }

    /// <summary>
    /// Clears the scene and loads elements and circuit from given Circuit data
    /// </summary>
    /// <param name="circuit">Circuit data to load</param>
    public void LoadCircuit(string circuitJsonText)
    {
        Logger.Debug($"Loading CircuitData:\n{circuitJsonText}");

        var circuit = Newtonsoft.Json.JsonConvert.DeserializeObject<CircuitData>(circuitJsonText);
        // var circuit = System.Text.Json.JsonSerializer.Deserialize<CircuitData>(circuitJsonText);

        Toolbar.Instance.Reset();
        UserInputController.Instance.ResetConnecting();
        Element.IsCurrentlyMoving = false;

        Circuit = circuit;
        BoundConnections.Clear();
        BoundElements.Clear();

        foreach (Node child in ElementContainerScene.GetChildren())
        {
            child.QueueFree();
        }

        ElementContainerScene.Position = Circuit.UserPosition.ToGodot();

        foreach (var eldata in Circuit.Elements)
        {
            var elementDef = ElementProvider.Instance.GetElementDefinition(eldata.Type);
            var newElement = elementDef.Scene.Instantiate<Element>();

            newElement.Data = eldata;
            newElement.Position = new Vector2(eldata.Position.X, eldata.Position.Y);
            newElement.Rotation = eldata.Rotation;

            CreateBoundElement(eldata, newElement);

            ElementContainerScene.AddChild(newElement);
        }

        foreach (var conn in Circuit.Connections)
        {
            var port1 = FindElementPort(conn.Port1);
            var port2 = FindElementPort(conn.Port2);

            var line = CreateLine2D(new Vector2[] { port1.Centroid, port2.Centroid });

            CreateBoundConnection(conn, port1, port2, line);
        }
    }

    /// <summary>
    /// Creates Line2D and adds it to ElementContainer
    /// </summary>
    /// <param name="points">Line points</param>
    /// <returns>Line2D object</returns>
    public Line2D CreateLine2D(Vector2[] points)
    {
        var line = new Line2D();

        line.Points = points;
        line.Width = 3f;

        ElementContainerScene.AddChild(line);

        return line;
    }

    /// <summary>
    /// Removes given element completely from Circuit (Scene, CircuitManager and Circuit data)
    /// </summary>
    /// <param name="element">Element to remove</param>
    public void RemoveElement(Element element)
    {
        foreach (ElementPort port in element.Ports)
        {
            var conns = BoundConnections.FindAll(x => (x.Port1.Data.Id == port.Data.Id || x.Port2.Data.Id == port.Data.Id));

            foreach (var conn in conns)
            {
                conn.Line.QueueFree();
                Circuit.Connections.Remove(conn.Data);
                BoundConnections.Remove(conn);
            }
        }

        BoundElements.RemoveAll(x => x.Element == element);
        Circuit.Elements.Remove(element.Data);

        element.QueueFree();
    }

    /// <summary>
    /// Generates new ID
    /// </summary>
    /// <returns>String of ID</returns>
    public string NewID() => (++Circuit.ID_ctr).ToString();

    /// <summary>
    /// Finds Elements of given type in BoundElements list
    /// </summary>
    /// <param name="type">Type of element</param>
    /// <returns>List of Elements with type</returns>
    public List<Element> FindElementsOfType(string type) => BoundElements.FindAll(x => x.Element.Data.Type == type).ConvertAll(x => x.Element);
}