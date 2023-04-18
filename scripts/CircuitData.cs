using System.Collections.Generic;

public class CircuitData
{
    public string Name; // Name of the circuit
    public Element Elements;
    public Connection Connections;
}

public record Connection
{
    public string Guid;
    public List<string> Ports;
}