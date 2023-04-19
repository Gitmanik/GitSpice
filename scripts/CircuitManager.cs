

using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public class CircuitManager
{
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private CircuitData Circuit = new CircuitData();

    public record BoundConnection
    {
        public ConnectionData Data;
        public ElementPort Port1;
        public ElementPort Port2;
        public Line2D Line;
    }
    public record BoundElement
    {
        public ElementData Data;
        public Element Element;
    }

    private List<BoundElement> BoundElements = new List<BoundElement>();
    private List<BoundConnection> BoundConnections = new List<BoundConnection>();

    /// <summary>
    /// Makes a connection. Throws ArgumentException when connection already exists.
    /// </summary>
    /// <param name="Port1">Port</param>
    /// <param name="Port2">Port</param>
    /// <returns>Connection object</returns>
    public ConnectionData ConnectPorts(string Port1, string Port2)
    {
        ConnectionData existingConnection = FindConnection(Port1, Port2);
        if (existingConnection != null)
        {
            throw new ArgumentException($"Tried to create already existing connection! (Port1: {Port1}, Port2: {Port2})");
        }

        ConnectionData conn = new ConnectionData(Port1, Port2);
        Circuit.Connections.Add(conn);
        return conn;
    }

    public ConnectionData FindConnection(string Port1, string Port2) => Circuit.Connections.Find(x => x.IsConnected(Port1) && x.IsConnected(Port2));

    public List<BoundConnection> FindBoundConnections(string Port) => BoundConnections.FindAll(x => x.Data.IsConnected(Port));

    public bool ConnectionExists(string Port1, string Port2) => FindConnection(Port1,Port2) != null;

    public void CreateElement(ElementData data)
    {
        if (Circuit.Elements.Contains(data))
        {
            throw new ArgumentException($"Tried to create already existing element! (ElementData: {data})");
        }
        Circuit.Elements.Add(data);
    }

    public void SaveToFile()
    {
        Console.WriteLine(JsonConvert.SerializeObject(Circuit));
    }

    public void BindConnection(ConnectionData conn, ElementPort Port1, ElementPort Port2, Line2D line)
    {
        BoundConnections.Add(new BoundConnection{Data = conn, Port1 = Port1, Port2 = Port2, Line = line});
    }

    internal void BindElement(ElementData eldata, Element element)
    {
        BoundElements.Add(new BoundElement{Data = eldata, Element = element});
    }
}