using System;
using System.Collections.Generic;
using Godot;

public partial class UserInputController : Control
{
    private const string CircuitSavePath = "user://circuit.json";
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static UserInputController Instance;

    private ElementPort CurrentlyConnecting;
    private Line2D ConnectingWire;
    private Control ElementContainerScene;

    private ElementDefinition PoleElementDef;

    //TODO: Make this configurable
    private Vector2 ScaleMultiplier = new Vector2(0.1f, 0.1f);

    public override void _Ready()
    {
        ElementContainerScene = GetNode<Control>(CircuitManager.ElementContainerPath);
        PoleElementDef = ElementProvider.Instance.GetElementDefinition("Pole");
        Instance = this;
    }

    bool mouseClickedBool = false;
    bool mouseDragging = false;
    Vector2 lastMouseDraggingPos;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Echo == false && key.Pressed == true)
        {
            if (key.Keycode == Key.C)
            {
                TestJunction();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (key.Keycode == Key.Backspace)
            {
                Logger.Info(CircuitManager.Instance.SaveCircuit());
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
                LoadCircuit();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (@event is InputEventMouseMotion mousemMoved && mouseClickedBool)
        {
            mouseDragging = true;
            Vector2 delta = mousemMoved.Position - lastMouseDraggingPos;

            ElementContainerScene.Position += delta;

            lastMouseDraggingPos = mousemMoved.Position;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventMouseButton mouseClicked)
        {
            if (mouseClicked.Pressed)
                lastMouseDraggingPos = mouseClicked.Position;

            mouseClickedBool = mouseClicked.Pressed;
            // Create new Element
            if (ConnectingWire == null && mouseClicked.ButtonIndex == MouseButton.Left && !mouseClicked.Pressed && !mouseDragging)
            {
                Logger.Debug("Creating new Element");

                if (Toolbar.Instance.SelectedElement == null)
                {
                    Logger.Debug("SelectedElement in Toolbar is null, returning");
                    return;
                }

                CreateElement(Toolbar.Instance.SelectedElement, RelativePosition(mouseClicked.Position));

                GetViewport().SetInputAsHandled();
                return;
            }

            // Create Pole
            if (ConnectingWire != null && mouseClicked.ButtonIndex == MouseButton.Left && !mouseClicked.Pressed)
            {
                var createdPole = CreateElement(PoleElementDef, SnapToGrid(RelativePosition(mouseClicked.Position)));

                ConnectingWireConnect(createdPole.Ports[0]);
                StartConnecting(createdPole.Ports[0]);

                GetViewport().SetInputAsHandled();
                return;
            }

            // Reset connecting
            if (ConnectingWire != null && mouseClicked.ButtonIndex == MouseButton.Right && !mouseClicked.Pressed)
            {
                ResetConnecting();
                GetViewport().SetInputAsHandled();
                return;
            }

            // Scale
            if (mouseClicked.ButtonIndex == MouseButton.WheelUp && !mouseClicked.Pressed && !Element.IsCurrentlyMoving)
            {
                Scale += ScaleMultiplier;
                Position -= RelativePosition(mouseClicked.Position) * ScaleMultiplier;
                GetViewport().SetInputAsHandled();
                return;
            }
            if (mouseClicked.ButtonIndex == MouseButton.WheelDown && !mouseClicked.Pressed && !Element.IsCurrentlyMoving)
            {
                Scale -= ScaleMultiplier;
                Position += RelativePosition(mouseClicked.Position) * ScaleMultiplier;
                GetViewport().SetInputAsHandled();
                return;
            }

            mouseDragging = false;
        }

        if (ConnectingWire != null && @event is InputEventMouseMotion mouseMoved)
        {
            ConnectingWire.Points = new Vector2[] { CurrentlyConnecting.Centroid, RelativePosition(mouseMoved.Position) };
        }
    }

    private void TestJunction()
    {
        Dictionary<Color, HashSet<string>> junctions = new Dictionary<Color, HashSet<string>>();

        byte randomByte() => Convert.ToByte(GD.Randi() % 256);
        Color randomColor() => Color.Color8(randomByte(), randomByte(), randomByte());

        foreach (var boundConn in CircuitManager.Instance.GetBoundConnections())
        {
            bool foundJunction = false;
            Logger.Debug($"Parsing BoundConnection: {boundConn.Port1.Data.Id} {boundConn.Port2.Data.Id}");
            foreach (var junction in junctions)
            {
                if (junction.Value.Contains(boundConn.Port1.Data.Id) || junction.Value.Contains(boundConn.Port2.Data.Id))
                {
                    Logger.Debug($"Adding to existing junction: {boundConn.Port1.Data.Id} {boundConn.Port2.Data.Id}");
                    junction.Value.Add(boundConn.Port1.Data.Id);
                    junction.Value.Add(boundConn.Port2.Data.Id);
                    foundJunction = true;
                }
            }
            if (!foundJunction)
            {
                Logger.Debug($"Created junction: {boundConn.Port1.Data.Id} {boundConn.Port2.Data.Id}");
                junctions.Add(randomColor(), new HashSet<string>() { boundConn.Port1.Data.Id, boundConn.Port2.Data.Id });
            }
        }

        foreach (var kvp in junctions)
        {
            foreach (string port in kvp.Value)
            {
                var conns = CircuitManager.Instance.FindBoundConnections(port);

                foreach (var conn in conns)
                {
                    conn.Line.DefaultColor = kvp.Key;
                }
            }
        }


        string debugJunctions = "";
        foreach (var kvp in junctions)
            debugJunctions += $"{kvp.Key}:\n{string.Join('\n', kvp.Value)}\n";
        Logger.Debug($"Generated junctions:\n{debugJunctions}");
    }

    private Element CreateElement(ElementDefinition elementDef, Vector2 position)
    {
        var elementData = ElementProvider.Instance.NewElementData(elementDef);
        var elementScene = CircuitManager.Instance.CreateElement(elementData);
        CircuitManager.Instance.MoveElement(elementScene, position);
        return elementScene;
    }

    public void ResetConnecting()
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
        CircuitManager.Instance.MoveElement(element, SnapToGrid(RelativePosition(e.GlobalPosition)));
    }

    private Vector2 SnapToGrid(Vector2 v)
    {
        //TODO: Make grid configurable
        v.X = Mathf.Round(v.X / 10f) * 10f;
        v.Y = Mathf.Round(v.Y / 10f) * 10f;
        return v;
    }

    /// <summary>
    /// Calculates position relative to moved ElementContainer (dragged by user)
    /// </summary>
    /// <param name="pos">Screen position</param>
    /// <returns>Relative position</returns>
    private Vector2 RelativePosition(Vector2 pos) => (pos - Position) / Scale;

    public void LoadCircuit()
    {
        Logger.Info($"Loading circuit from {CircuitSavePath}");
        var circuitFile = FileAccess.Open(CircuitSavePath, FileAccess.ModeFlags.Read);
        var circuitJsonText = circuitFile.GetAsText();
        circuitFile.Close();

        CircuitManager.Instance.LoadCircuit(circuitJsonText);
    }

    public void SaveCircuit()
    {
        Logger.Info("Saving current Circuit state");

        var circuitFile = FileAccess.Open(CircuitSavePath, FileAccess.ModeFlags.Write);
        var circuitJsonText = CircuitManager.Instance.SaveCircuit();

        circuitFile.StoreString(circuitJsonText);
        circuitFile.Close();

        Logger.Debug($"Saved CircuitData to file {CircuitSavePath}:\n{circuitJsonText}");
    }
}
