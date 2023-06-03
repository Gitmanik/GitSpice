namespace Gitmanik.Models;

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
