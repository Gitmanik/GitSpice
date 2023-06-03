using System.Collections.Generic;
using System.Numerics;

namespace Gitmanik.Models;

public class CircuitData
{
    public int ID_ctr;
    public List<ElementData> Elements = new List<ElementData>();
    public List<ConnectionData> Connections = new List<ConnectionData>();
    public Vector2 UserPosition = Vector2.Zero;
    public float UserZoom = 1f;
}