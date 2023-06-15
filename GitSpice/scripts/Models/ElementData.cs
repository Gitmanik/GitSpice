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
                return null;
            case "Pole":
                return null;
            default:
                throw new NotImplementedException();
        }
    }

    public string GetCurrent()
    {
        switch (Type)
        {
            case "Voltage_Source":
                return $"Iu{Id}";
            case "Resistor":
                return $"Ir{Id}";
            case "Current_Source":
                return $"I{Id}";
            case "Pole":
                return null;
            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns all known variables
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, string> GetAllGivens()
    {
        Dictionary<string, string> givens = new Dictionary<string, string>();

        switch (Type)
        {
            case "Voltage_Source":
                return new Dictionary<string, string>()
                {
                    {$"U{Id}", Data["Amount"]}
                };
            case "Resistor":
                return new Dictionary<string, string>()
                {
                    {$"R{Id}", Data["Resistance"]},
                    {$"Ur{Id}", $"R{Id}*Ir{Id}"}
                };
            case "Current_Source":
                return new Dictionary<string, string>()
                {
                    {$"I{Id}", Data["Amount"]},
                };
            case "Pole":
                return null;
            default:
                throw new NotImplementedException();
        }
    }
}