using Godot;
using Newtonsoft.Json;

public partial class UserInputController : Control
{
    private const string CircuitSavePath = "user://circuit.json";
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static UserInputController Instance;

    private ElementPort CurrentlyConnecting;
    private Line2D ConnectingWire;

    private Control ElementContainerScene;

    public override void _Ready()
    {
        ElementContainerScene = GetNode<Control>(CircuitManager.ElementContainerPath);

        Instance = this;
    }

    bool mouseClickedBool = false;
    Vector2 lastMousePos;
    public override void _UnhandledInput(InputEvent @event)
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
                ResetConnecting();
                Toolbar.Instance.Reset();
                LoadCircuit();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (@event is InputEventMouseMotion mousemMoved && mouseClickedBool)
        {
            Vector2 delta = mousemMoved.Position - lastMousePos;

            ElementContainerScene.Position += delta;

            lastMousePos = mousemMoved.Position;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventMouseButton mouseClicked)
        {
            if (mouseClicked.Pressed)
                lastMousePos = mouseClicked.Position;

            mouseClickedBool = mouseClicked.Pressed;
            // Create new Element
            if (ConnectingWire == null && mouseClicked.ButtonIndex == MouseButton.Left && !mouseClicked.Pressed)
            {
                Logger.Debug("Creating new Element");

                if (Toolbar.Instance.SelectedElement == null)
                {
                    Logger.Debug("SelectedElement in Toolbar is null, returning");
                    return;
                }

                CreateElement(Toolbar.Instance.SelectedElement, mouseClicked.Position);

                GetViewport().SetInputAsHandled();
                return;
            }

            // Create Pole
            if (ConnectingWire != null && mouseClicked.ButtonIndex == MouseButton.Left && !mouseClicked.Pressed)
            {
                //TODO: Cache pole elementdef
                var createdPole = CreateElement(ElementProvider.Instance.GetElementDefinition("Pole"), mouseClicked.Position);

                ConnectingWireConnect(createdPole.Ports[0]);
                StartConnecting(createdPole.Ports[0]);

                GetViewport().SetInputAsHandled();
                return;
            }

            // Reset connecting
            if (ConnectingWire != null && mouseClicked.ButtonIndex == MouseButton.Right && !mouseClicked.Pressed)
            {
                //TODO: Remove all Poles
                // var x = CurrentlyConnecting;
                // while (x.ParentElement.Data.Type == "Pole")
                // {
                //     CircuitManager.Instance.
                // }

                ResetConnecting();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (ConnectingWire != null && @event is InputEventMouseMotion mouseMoved)
        {
            ConnectingWire.Points = new Vector2[] { CurrentlyConnecting.Centroid, mouseMoved.GlobalPosition };
        }
    }

    private Element CreateElement(ElementDefinition elementDef, Vector2 position)
    {
        var elementData = ElementProvider.Instance.NewElementData(elementDef);
        var elementScene = CircuitManager.Instance.CreateElement(elementData);
        CircuitManager.Instance.MoveElement(elementScene, position);
        return elementScene;
    }

    private void ResetConnecting()
    {
        if (ConnectingWire != null)
            ConnectingWire.QueueFree();

        ConnectingWire = null;
        CurrentlyConnecting = null;
    }

    public void ElementPortClicked(ElementPort clickedPort)
    {
        if (CurrentlyConnecting != null)
            ConnectingWireConnect(clickedPort);
        else
            StartConnecting(clickedPort);
    }

    private void ConnectingWireConnect(ElementPort clickedPort)
    {
        if (!CircuitManager.Instance.ConnectionExists(CurrentlyConnecting.Data.Id, clickedPort.Data.Id))
        {
            CircuitManager.Instance.ConnectPorts(CurrentlyConnecting, clickedPort);
        }
        else
            Logger.Warn($"Tried to create already existing connection! (Port1: {CurrentlyConnecting.Data.Id}, Port2: {clickedPort.Data.Id})");

        ResetConnecting();
    }

    private void StartConnecting(ElementPort clickedPort)
    {
        CurrentlyConnecting = clickedPort;

        ConnectingWire = CircuitManager.Instance.CreateLine2D(new Vector2[] { CurrentlyConnecting.Centroid });
        ConnectingWire.DefaultColor = Colors.LawnGreen;
    }

    public void MoveElement(Element element, InputEventMouseMotion e)
    {
        CircuitManager.Instance.MoveElement(element, e.GlobalPosition - ElementContainerScene.Position);
        GetViewport().SetInputAsHandled();
    }

    public void LoadCircuit()
    {
        ResetConnecting();
        ElementContainerScene.Position = Vector2.Zero;

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
