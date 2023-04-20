using System;
using System.Collections.Generic;
using System.Numerics;

public class CircuitData
{
    public string Name;
    public List<ElementData> Elements = new List<ElementData>();
    public List<ConnectionData> Connections = new List<ConnectionData>();

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
        Connections.Add(conn);
        return conn;
    }

    /// <summary>
    /// Finds ConnectionData object between ports
    /// </summary>
    /// <param name="Port1">Port</param>
    /// <param name="Port2">Port</param>
    /// <returns>ConnectionData object</returns>
    public ConnectionData FindConnection(string Port1, string Port2) => Connections.Find(x => x.IsConnected(Port1) && x.IsConnected(Port2));

    /// <summary>
    /// Checks if Connection between ports exists.
    /// </summary>
    /// <param name="Port1">Port</param>
    /// <param name="Port2">Port</param>
    /// <returns>True if connection exists, otherwise false</returns>
    public bool ConnectionExists(string Port1, string Port2) => FindConnection(Port1, Port2) != null;


}

public class ConnectionData
{
    private ConnectionData() { }
    public ConnectionData(string Port1, string Port2)
    {
        this.Port1 = Port1;
        this.Port2 = Port2;
        Id = System.Guid.NewGuid().ToString();
    }
    public readonly string Id;
    public readonly string Port1;
    public readonly string Port2;

    public bool IsConnected(string port) => Port1 == port || Port2 == port;
}

public class ElementData
{
    private ElementData() { }
    public ElementData(string Type)
    {
        this.Type = Type;
        Id = System.Guid.NewGuid().ToString();
    }
    public readonly string Id;
    public readonly string Type;
    public Dictionary<string, string> Data;

    public Vector2 Position;

    public List<PortData> Ports;
}

public class PortData
{
    public PortData()
    {
        Id = System.Guid.NewGuid().ToString();
    }

    public string Id;
}