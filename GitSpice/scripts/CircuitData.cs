using System.Collections.Generic;
using System.Numerics;

public class CircuitData
{
    public int ID_ctr;
    public List<ElementData> Elements = new List<ElementData>();
    public List<ConnectionData> Connections = new List<ConnectionData>();
    public Vector2 UserPosition = Vector2.Zero;
    public float UserZoom = 1f;
}

public class ConnectionData
{
    public ConnectionData() { }
    public ConnectionData(string Port1, string Port2)
    {
        this.Port1 = Port1;
        this.Port2 = Port2;
        Id = CircuitManager.Instance.NewID();
    }

    public string Id;
    public string Port1;
    public string Port2;

    public bool IsConnected(string port) => Port1 == port || Port2 == port;
}

public class ElementData
{
    public ElementData() { }
    public ElementData(string Type)
    {
        this.Type = Type;
        Id = CircuitManager.Instance.NewID();
    }

    public string Id;
    public string Type;
    public Dictionary<string, string> Data;
    public Vector2 Position;
    public float Rotation;
    public List<PortData> Ports;
}

public class PortData
{
    public PortData()
    {
        Id = CircuitManager.Instance.NewID();
    }

    public string Id;
}