using System;
using System.Collections.Generic;
using Godot;
using Gitmanik.Utils.Extensions;
using System.Linq;
using Gitmanik.Models;
using Gitmanik.Controllers;
using System.Text.Json;

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

    public override void _EnterTree()
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
        if (Port1.PortId == Port2.PortId)
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
            var port = element.Element.Ports.Find(x => x.PortId == Port);
            if (port != null)
                return port;
        }
        return null;
    }

    /// <summary>
    /// Finds all connected ports to given Port Id
    /// </summary>
    /// <param name="portId"Port Id</param>
    /// <returns>List od Port Ids</returns>
    List<string> GetConnectedPorts(string portId) => FindBoundConnections(portId).ConvertAll(x => portId == x.Port1.PortId ? x.Port2.PortId : x.Port1.PortId);

    /// <summary>
    /// Returns all ports in Circuit
    /// </summary>
    /// <returns>List of Port Ids</returns>
    List<string> GetAllPorts()
    {
        List<string> allPorts = new List<string>();
        foreach (var l in Circuit.Elements.ConvertAll(x => x.Ports))
            allPorts.AddRange(l);
        return allPorts;
    }

    /// <summary>
    /// Created an element in CircuitData and its visual representation.
    /// Creates Element object (based on Type) and adds it to scene.
    /// Throws ArgumentException when element already exists.
    /// </summary>
    /// <param name="data">Element data</param>
    /// <returns>Element object</returns>
    public Element CreateElement(ElementData data, Vector2 position)
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
        MoveElement(elementScene, position);

        return elementScene;
    }

    public Element CreateElement(ElementDefinition elementDef, Vector2 position) => CreateElement(ElementProvider.Instance.NewElementData(elementDef), position);

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
        ConnectionData existingConnection = FindConnection(Port1.PortId, Port2.PortId);
        if (existingConnection != null)
        {
            throw new ArgumentException($"Tried to create already existing connection! (Port1: {Port1}, Port2: {Port2})");
        }

        ConnectionData conn = new ConnectionData(Port1.PortId, Port2.PortId);
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
    /// Finds BoundConnection object between ports
    /// </summary>
    /// <param name="Port1">Port</param>
    /// <param name="Port2">Port</param>
    /// <returns>BoundConnection object</returns>
    public BoundConnection FindBoundConnection(string Port1, string Port2) => BoundConnections.Find(x => (x.Data.IsConnected(Port1) && x.Data.IsConnected(Port2)));

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
            List<CircuitManager.BoundConnection> connections = FindBoundConnections(port.PortId);
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
        Circuit.UserZoom = ElementContainerScene.Scale.X;
        return JsonSerializer.Serialize(Circuit, options: new JsonSerializerOptions() { IncludeFields = true });
    }

    /// <summary>
    /// Clears the scene and loads elements and circuit from given Circuit data
    /// </summary>
    /// <param name="circuit">Circuit data to load</param>
    public void LoadCircuit(string circuitJsonText)
    {
        Logger.Debug($"Loading CircuitData:\n{circuitJsonText}");

        Toolbar.Instance.Reset();
        UserInputController.Instance.ResetConnecting();
        Element.IsCurrentlyMoving = false;
        BoundConnections.Clear();
        BoundElements.Clear();

        foreach (Node child in ElementContainerScene.GetChildren())
        {
            child.QueueFree();
        }

        if (circuitJsonText == null)
            Circuit = new CircuitData();
        else
            try
            {
                Circuit = JsonSerializer.Deserialize<CircuitData>(circuitJsonText, options: new JsonSerializerOptions() { IncludeFields = true });
            }
            catch (JsonException e)
            {
                Logger.Error($"Error while loading circuit: {e}");
                Circuit = new CircuitData();
            }

        ElementContainerScene.Scale = new Vector2(Circuit.UserZoom, Circuit.UserZoom);
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
            var conns = BoundConnections.FindAll(x => (x.Port1.PortId == port.PortId || x.Port2.PortId == port.PortId));

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

    /// <summary>
    /// Returns all Elements in BoundElements list
    /// </summary>
    /// <returns></returns>
    public List<Element> GetElements() => BoundElements.ConvertAll(x => x.Element);

    /// <summary>
    /// Calculates all nodes in Circuit
    /// </summary>
    /// <returns>List of HashMap of Port IDs in junction</returns>
    public List<HashSet<string>> CalculateNodes()
    {
        List<HashSet<string>> junctions = new List<HashSet<string>>();

        foreach (var boundConn in CircuitManager.Instance.GetBoundConnections())
        {
            bool foundJunction = false;
            Logger.Debug($"Parsing BoundConnection: {boundConn.Port1.PortId} {boundConn.Port2.PortId}");
            foreach (var junction in junctions)
            {
                if (junction.Contains(boundConn.Port1.PortId) || junction.Contains(boundConn.Port2.PortId))
                {
                    Logger.Debug($"Adding to existing junction: {boundConn.Port1.PortId} {boundConn.Port2.PortId}");
                    junction.Add(boundConn.Port1.PortId);
                    junction.Add(boundConn.Port2.PortId);
                    foundJunction = true;
                }
            }
            if (!foundJunction)
            {
                Logger.Debug($"Created junction: {boundConn.Port1.PortId} {boundConn.Port2.PortId}");
                junctions.Add(new HashSet<string>() { boundConn.Port1.PortId, boundConn.Port2.PortId });
            }
        }
        return junctions;
    }

    /// <summary>
    /// Returns port Ids between beginning port and ending
    /// </summary>
    /// <param name="begin">Beginning port Id</param>
    /// <param name="end">Ending port Id</param>
    /// <returns></returns>
    public List<string> CalculateLoop(string begin, string end)
    {
        Logger.Debug($"Calculating loop {begin} -> {end}");

        if (Circuit.Elements.Count == 0)
            return null;

        Queue<string> queue = new Queue<string>();

        HashSet<string> visited = new HashSet<string>();
        Dictionary<string, string> predecessor = new Dictionary<string, string>();

        queue.Enqueue(begin);
        visited.Add(begin);

        bool firstIteration = true;

        int distanceCtr = 0;
        while (queue.Count != 0)
        {
            var currentPort = queue.Dequeue();

            Logger.Trace($"Visited {currentPort}");
            if (currentPort == end)
            {
                Logger.Trace($"Found loop");
                break;
            }

            ElementData currentElement = CircuitManager.Instance.FindElementPort(currentPort).ParentElement.Data;

            var searchingPort = currentPort;
            if (currentElement.Ports.Count > 1 && !firstIteration)
            {
                searchingPort = currentElement.Ports[0] == currentPort ? currentElement.Ports[1] : currentElement.Ports[0];
                Logger.Trace($"Setting searchingPort: {currentPort} -> {searchingPort}");
                if (!visited.Contains(searchingPort))
                {
                    Logger.Trace($"Setting {searchingPort} as visited");
                    predecessor.Add(searchingPort, currentPort);
                    visited.Add(searchingPort);
                }
            }

            firstIteration = false;

            foreach (var p in CircuitManager.Instance.FindBoundConnections(searchingPort))
            {
                string newPort = searchingPort == p.Port1.PortId ? p.Port2.PortId : p.Port1.PortId;

                Logger.Trace($"New port {currentPort} {searchingPort}: {newPort}");
                if (visited.Contains(newPort))
                    continue;

                Logger.Trace($"Enqueueing: {newPort}");
                predecessor.Add(newPort, searchingPort);
                visited.Add(newPort);
                queue.Enqueue(newPort);
            }
            distanceCtr++;
        }

        foreach (var kvp in predecessor)
            Logger.Trace($"Predecessor of {kvp.Key}: {kvp.Value}");

        List<string> path = new List<string>();

        path.Add(end);
        string curr = end;

        while (predecessor.ContainsKey(curr))
        {
            Logger.Trace($"{curr} -> {predecessor[curr]}");
            path.Add(predecessor[curr]);
            curr = predecessor[curr];
        }

        path.Reverse();

        Logger.Debug($"Loop {begin}->{end}: {string.Join(',', path)}");

        return path;
    }

    public Dictionary<string, string> CalculateCurrentSymbols()
    {
        string TraverseElement(ElementData el, string portId)
        {
            var l = el.Ports.Find(x => x != portId);
            return l ?? portId;
        }
        string GetElementIdForPort(string portId) => FindElementPort(portId).ParentElement.Data.Id;

        List<List<string>> all = new List<List<string>>();

        foreach (string port in GetAllPorts())
        {
            Logger.Trace("------");
            List<string> elementPath = new List<string>();
            string currentPort = port;
            while (true)
            {
                Logger.Trace($"Current port: {currentPort}");

                if (elementPath.Contains(currentPort))
                {
                    Logger.Trace("Loop detected. Breaking.");
                    break;
                }

                var connectedPorts = GetConnectedPorts(currentPort);
                string currentElementId = GetElementIdForPort(currentPort);
                ElementData currentElement = Circuit.Elements.Find(x => x.Id == currentElementId);
                string connectedPort = null;
                if (connectedPorts.Count > 1 && currentElement.Type != "Pole")
                {
                    Logger.Trace($" {currentElementId}:{currentPort} is connected to more than one port. Breaking loop.");
                    break;
                }
                else if (currentElement.Type == "Pole")
                {
                    Logger.Trace($"Current element {currentElementId}:{currentPort} is Pole");
                    if (connectedPorts.Count > 2)
                    {
                        Logger.Trace($"Pole {currentElementId}:{currentPort} is connected to more than two ports. Breaking loop.");
                        break;
                    }
                    Logger.Trace($"Adding {currentElementId} to list");
                    elementPath.Add(currentPort);
                    connectedPort = connectedPorts.Find(x => x != currentPort);
                }
                else if (connectedPorts.Count == 0)
                {
                    Logger.Trace($"Port {currentPort} is not connected to anything. Breaking loop");
                    break;
                }
                else
                {
                    Logger.Trace($"Adding {currentElementId} to list");
                    // elementPath.Add(currentElementId);
                    elementPath.Add(currentPort);
                    connectedPort = connectedPorts[0];
                }
                ElementData connectedElement = FindElementPort(connectedPort).ParentElement.Data;
                if (connectedElement.Type == "Pole")
                {
                    Logger.Trace($"Connected element {connectedElement.Id} is Pole");
                    var connectedToPole = GetConnectedPorts(connectedPort);
                    Logger.Trace($"Adding Pole {connectedElement.Id} to list");
                    // elementPath.Add(connectedElement.Id);
                    elementPath.Add(connectedPort);
                    if (connectedToPole.Count > 2)
                    {
                        Logger.Trace("Connected Pole is connected to more than two elements. Breaking loop");
                        break;
                    }
                    string t = connectedToPole.Find(x => x != currentPort);
                    Logger.Trace($"Traversing Pole {connectedElement.Id} from {connectedPort} to {t}");
                    connectedPort = t;
                }

                Logger.Trace($"Connected port to {currentElementId}: {connectedPort}");
                Logger.Trace($"Adding port {connectedPort} to list");
                elementPath.Add(connectedPort);
                string traversed = TraverseElement(FindElementPort(connectedPort).ParentElement.Data, connectedPort);
                Logger.Trace($"Traversing from {currentPort} to {traversed}");
                currentPort = traversed;
            }

            Logger.Trace($"Generated path: {string.Join(',', elementPath)}");
            if (elementPath.Count > 0)
                all.Add(elementPath);
        }

        List<List<string>> generatedPaths = new List<List<string>>();

        bool IsSameElement(string port1, string port2) => Circuit.Elements.Any(x => (x.Ports.Count == 1 && x.Ports[0] == port1 && port1 == port2) || (x.Ports.Count == 2 && ((x.Ports[0] == port1 && x.Ports[1] == port2) || (x.Ports[0] == port2 && x.Ports[1] == port1))));

        foreach (List<string> elementPath1 in all)
        {
            foreach (List<string> elementPath2 in all)
            {
                if (elementPath1 == elementPath2)
                    continue;
                if (IsSameElement(elementPath1[0], elementPath2[0]))
                {
                    List<string> reversedElementPath2 = new List<string>(elementPath2); reversedElementPath2.Reverse();

                    List<string> fullPath = new List<string>(reversedElementPath2);
                    fullPath.AddRange(elementPath1);

                    if (!generatedPaths.Any(x => x.Intersect(fullPath).Count() == x.Count()))
                    {
                        generatedPaths.Add(fullPath.ToHashSet().ToList());
                        Logger.Trace($"Connected {string.Join(',', elementPath1)} with {string.Join(',', elementPath2)}");
                    }
                    break;
                }
            }
        }

        Logger.Trace("All generated paths");
        foreach (List<string> path in generatedPaths)
        {
            Logger.Trace(string.Join(',', path));
        }

        Dictionary<string, string> symbols = new Dictionary<string, string>();

        Logger.Trace("Assigning symbols in paths");
        int currentSymbolCtr = 1;
        foreach (List<string> path in generatedPaths)
        {
            string symbol = $"I{currentSymbolCtr}";
            Logger.Trace($"Parsing path {string.Join(',', path)} with symbol {symbol}");
            List<string> path_copy = new List<string>(path);

            while (path_copy.Count > 0)
            {
                string port = path_copy[0];
                path_copy.RemoveAt(0);
                ElementData element = FindElementPort(port).ParentElement.Data;

                Logger.Trace($"Current port: {port}, Element id: {element.Id}, type: {element.Type}");

                if (symbols.ContainsKey(element.Id))
                {
                    Logger.Trace($"Symbol already assigned to {element.Id}, skipping");
                    continue;
                }

                if (element.Type == "Pole")
                    continue;

                symbols.Add(element.Id, symbol);
            }
            currentSymbolCtr++;
        }
        Logger.Debug("Generated symbols");
        foreach (var kvp in symbols)
            Logger.Debug($"{kvp}");

        return symbols;
    }

    public void ColorLoop(List<string> loop, Color c)
    {
        for (int idx = 0; idx < loop.Count - 1; idx++)
        {
            var port1 = loop[idx];
            var port2 = loop[idx + 1];
            Logger.Trace($"Ports: {port1} {port2}");
            if (GetElements().Any(e => e.Data.Ports.SequenceEqual(new List<string>() { port1, port2 }) || e.Data.Ports.SequenceEqual(new List<string>() { port2, port1 })))
            {
                Logger.Trace("Skipping element");
                continue;
            }
            var boundConnection = FindBoundConnection(port1, port2);
            if (boundConnection == null)
            {
                Logger.Error($"Could not find bound connection for {port1} -> {port2}!");
                continue;
            }
            boundConnection.Line.DefaultColor = c;
        }
    }

    public string Calculate2ndKirchhoffLaw(List<string> loop)
    {
        List<string> voltages = new List<string>();
        string equation = "";
        loop.Insert(0, loop[loop.Count - 1]);
        for (int idx = 0; idx < loop.Count - 1; idx++)
        {
            var port1 = loop[idx + 1];
            var port2 = loop[idx];
            Logger.Trace($"Ports: {port1} {port2}");

            Element e = CircuitManager.Instance.GetElements().Find(e => e.Data.Ports.SequenceEqual(new List<string>() { port1, port2 }) || e.Data.Ports.SequenceEqual(new List<string>() { port2, port1 }));
            if (e == null)
            {
                Logger.Trace("Skipping non-element");
                continue;
            }
            Logger.Trace($"Element ports: {e.Ports[0].PortId} {e.Ports[1].PortId}, ports: {port1} {port2}");

            string sign = "+";
            if (e.Ports[0].PortId != port1) // TODO: Make more sophisticated signing
                sign = "-";

            string eq_part = $" {sign} {e.Data.GetVoltage()}";
            if (e.Data.Type == "Resistor")
                voltages.Add(e.Data.GetVoltage());
            Logger.Debug($"Equation part for {e.Data.Id}: {eq_part}");
            equation += eq_part;
        }

        equation += " = 0";

        Logger.Debug($"Calculated 2nd law for loop: {equation}");
        return equation;
    }

    public List<ElementData> GetAllElementsInLoop(List<string> loop)
    {
        List<ElementData> elements = new List<ElementData>();

        loop.Insert(0, loop[loop.Count - 1]);
        var all = new Dictionary<string, string>();
        for (int idx = 0; idx < loop.Count - 1; idx++)
        {
            var port1 = loop[idx + 1];
            var port2 = loop[idx];
            Logger.Trace($"Ports: {port1} {port2}");

            Element e = CircuitManager.Instance.GetElements().Find(e => e.Data.Ports.SequenceEqual(new List<string>() { port1, port2 }) || e.Data.Ports.SequenceEqual(new List<string>() { port2, port1 }));
            if (e == null)
            {
                Logger.Trace("Skipping non-element");
                continue;
            }
            Logger.Trace($"Element ports: {e.Ports[0].PortId} {e.Ports[1].PortId}, ports: {port1} {port2}");

            elements.Add(e.Data);
        }

        return elements;
    }

    public string SolveLinearSystem(List<string> equations, Element solveFor)
    {
        string command = $"ev({solveFor.Data.GetVoltage()}, solve([{string.Join(", ", equations)}]));";

        Logger.Debug($"Maxima command: {command}");

        return AppController.Maxima.Evaluate(command);
    }
}