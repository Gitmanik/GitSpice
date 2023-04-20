

using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public class CircuitManager
{
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public CircuitData Circuit = new CircuitData();

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

    public List<BoundConnection> FindBoundConnections(string Port) => BoundConnections.FindAll(x => x.Data.IsConnected(Port));

    public void CreateElement(ElementData data)
    {
        if (Circuit.Elements.Contains(data))
        {
            throw new ArgumentException($"Tried to create already existing element! (ElementData: {data})");
        }
        Circuit.Elements.Add(data);
    }

    public string SerializeCircuitToJson() => JsonConvert.SerializeObject(Circuit);

    public void BindConnection(ConnectionData conn, ElementPort Port1, ElementPort Port2, Line2D line)
    {
        BoundConnections.Add(new BoundConnection{Data = conn, Port1 = Port1, Port2 = Port2, Line = line});
    }

    public void BindElement(ElementData eldata, Element element)
    {
        BoundElements.Add(new BoundElement{Data = eldata, Element = element});
        element.Data = eldata;
    }
}