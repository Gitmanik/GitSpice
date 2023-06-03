namespace Gitmanik.Models;

public class PortData
{
    public PortData()
    {
        Id = CircuitManager.Instance.NewID();
    }

    public string Id;
}