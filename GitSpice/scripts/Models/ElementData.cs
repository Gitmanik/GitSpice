using System;
using System.Collections.Generic;
using System.Numerics;

namespace Gitmanik.Models;

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
    public List<string> Ports;

    public string GetVoltage()
    {
        switch (Type)
        {
            case "Voltage_Source":
                return $"U{Id}";
            case "Resistor":
                return $"Ur{Id}";
            case "Current_Source":
                return $"U{Id}";
            case "Pole":
                return null;
            default:
                throw new NotImplementedException();
        }
    }

    public string GetCurrent()
    {
        return CircuitManager.Instance.CalculateCurrentSymbols()[Id];
    }

    /// <summary>
    /// Returns all values associated with this Element
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, string> GetAllValues()
    {
        switch (Type)
        {
            case "Voltage_Source":
                return new Dictionary<string, string>()
                {
                    {GetVoltage(), Data["Amount"]}
                };
            case "Resistor":
                return new Dictionary<string, string>()
                {
                    {$"R{Id}", Data["Resistance"]},
                    {GetVoltage(), $"R{Id}*{GetCurrent()}"}
                };
            case "Current_Source":
                return new Dictionary<string, string>()
                {
                    {GetCurrent(), Data["Amount"]}
                };
            case "Pole":
                return null;
            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Checks if current flow is in right direction (from left to right)
    /// </summary>
    /// <param name="port1">Port on the left of connection</param>
    /// <returns>True if port is on the left</returns>
    public bool CurrentDirection(string port)
    {
        return port == Ports[0];
    }

    public string Traverse(string port)
    {
        if (Ports.Count == 1)
            return port;
        if (port == Ports[0])
            return Ports[1];
        else return Ports[0];
    }

}
