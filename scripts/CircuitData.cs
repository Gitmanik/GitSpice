using System;
using System.Collections.Generic;

public class CircuitData
{
    public string Name;
    public List<ElementData> Elements = new List<ElementData>();
    public List<ConnectionData> Connections = new List<ConnectionData>();
}

public class ConnectionData
{
    private ConnectionData() {}
    public ConnectionData(string Port1, string Port2) {
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
    private ElementData() {}
    public ElementData(string Type) {
        this.Type = Type;
        Id = System.Guid.NewGuid().ToString();
    }
    public readonly string Id;
    public readonly string Type;
    public Dictionary<string,string> Data = new Dictionary<string, string>();
    
    public Dictionary<string, PortData> Ports = new Dictionary<string, PortData>();
}

public class PortData
{
    public string Id;
    public List<PortConnection> Connections = new List<PortConnection>();
}

public record PortConnection
{
    public string Id;
    public string Port1_Id;
    public string Port2_Id;
}