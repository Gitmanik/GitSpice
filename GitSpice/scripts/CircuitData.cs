using System.Collections.Generic;
using System.Numerics;

public class CircuitData
{
    private static int ID_ctr = 0;
    public static string NewID() => (++ID_ctr).ToString();
    // public static string NewID() => System.Guid.NewGuid().ToString();
    public string Name;
    public List<ElementData> Elements = new List<ElementData>();
    public List<ConnectionData> Connections = new List<ConnectionData>();
}

public class ConnectionData
{
    private ConnectionData() { }
    public ConnectionData(string Port1, string Port2)
    {
        this.Port1 = Port1;
        this.Port2 = Port2;
        Id = CircuitData.NewID();
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
        Id = CircuitData.NewID();
    }

    public readonly string Id;
    public readonly string Type;
    public Dictionary<string, string> Data;
    public Vector2 Position;
    public float Rotation;
    public List<PortData> Ports;
}

public class PortData
{
    public PortData()
    {
        Id = CircuitData.NewID();
    }

    public string Id;
}