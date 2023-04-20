using Godot;
using Newtonsoft.Json;

public partial class UserInputController : Control
{
    private const string CircuitSavePath = "user://circuit.json";
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static UserInputController Instance;

    private ElementPort CurrentlyConnecting;
    private Line2D ConnectingWire;

    private Node ElementContainerScene;

    public override void _Ready()
    {
        ElementContainerScene = GetNode(CircuitManager.ElementContainerPath);

        Instance = this;
    }


    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Echo == false && key.Pressed == true)
        {
            if (key.Keycode == Key.Backspace)
            {
                Logger.Warn(JsonConvert.SerializeObject(CircuitManager.Instance.Circuit));
                GetViewport().SetInputAsHandled();
                return;
            }
            if (key.Keycode == Key.S)
            {
                SaveCircuit();
                GetViewport().SetInputAsHandled();
                return;
            }
            if (key.Keycode == Key.L)
            {
                LoadCircuit();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (ConnectingWire != null && @event is InputEventMouseButton mouseClicked && mouseClicked.ButtonIndex == MouseButton.Right)
        {
            ResetConnecting();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (ConnectingWire != null && @event is InputEventMouseMotion mouseMoved)
        {
            ConnectingWire.Points = new Vector2[] { CurrentlyConnecting.OffsetPosition, mouseMoved.GlobalPosition };
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton e && e.ButtonIndex == MouseButton.Left && e.Pressed)
        {
            Logger.Debug("Creating new Element");

            if (Toolbar.Instance.SelectedElement == null)
            {
                Logger.Debug("SelectedElement in Toolbar is null, returning");
                return;
            }

            var elementData = ElementProvider.Instance.NewElementData(Toolbar.Instance.SelectedElement);
            var elementScene = CircuitManager.Instance.CreateElement(elementData);
            CircuitManager.Instance.MoveElement(elementScene, e.Position);
        }
    }

    private void ResetConnecting()
    {
        if (ConnectingWire != null)
            ConnectingWire.QueueFree();

        ConnectingWire = null;
        CurrentlyConnecting = null;
    }

    public void ConnectClicked(ElementPort clickedPort)
    {
        if (CurrentlyConnecting != null)
        {
            if (!CircuitManager.Instance.ConnectionExists(CurrentlyConnecting.Data.Id, clickedPort.Data.Id))
            {
                CircuitManager.Instance.ConnectPorts(CurrentlyConnecting, clickedPort);
            }
            else
                Logger.Warn($"Tried to create already existing connection! (Port1: {CurrentlyConnecting.Data.Id}, Port2: {clickedPort.Data.Id})");

            ResetConnecting();
        }
        else
        {
            CurrentlyConnecting = clickedPort;

            ConnectingWire = new Line2D();
            ConnectingWire.Points = new Vector2[] { CurrentlyConnecting.OffsetPosition };
            ConnectingWire.DefaultColor = Color.Color8(255, 0, 0);
            ElementContainerScene.AddChild(ConnectingWire);
        }
    }

    public void MoveElement(Element element, InputEventMouseMotion e)
    {
        CircuitManager.Instance.MoveElement(element, e.Position);
        GetViewport().SetInputAsHandled();
    }

    public void LoadCircuit()
    {
        var circuitFile = FileAccess.Open(CircuitSavePath, FileAccess.ModeFlags.Read);
        var circuitJsonText = circuitFile.GetAsText();
        circuitFile.Close();

        Logger.Debug($"Loading CircuitData:\n{circuitJsonText}");

        var circuit = JsonConvert.DeserializeObject<CircuitData>(circuitJsonText);

        CircuitManager.Instance.LoadCircuit(circuit);
    }

    public void SaveCircuit()
    {
        Logger.Info("Saving current Circuit state");

        var circuitFile = FileAccess.Open(CircuitSavePath, FileAccess.ModeFlags.Write);
        var circuitJsonText = JsonConvert.SerializeObject(CircuitManager.Instance.Circuit);

        circuitFile.StoreString(circuitJsonText);
        circuitFile.Close();

        Logger.Debug($"Saved CircuitData to file {CircuitSavePath}:\n{circuitJsonText}");
    }
}
