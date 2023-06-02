using System;
using System.Collections.Generic;
using System.IO;
using Gitmanik.Controllers;
using Gitmanik.Math;
using Gitmanik.Utils;
using Godot;

public partial class UserInputController : Control
{
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static UserInputController Instance;

    private ElementPort CurrentlyConnecting;
    private Line2D ConnectingWire;
    private Control ElementContainerScene;

    private ElementDefinition PoleElementDef;

    //TODO: Make this configurable
    private Vector2 ScaleMultiplier = new Vector2(0.1f, 0.1f);

    private string circuitPath = null;

    //TODO: Make this configurable
    private const string PathToMaxima = @"C:\maxima-5.46.0\bin\maxima.bat";
    public MaximaService Maxima;

    public override void _Ready()
    {
        ElementContainerScene = GetNode<Control>(CircuitManager.ElementContainerPath);
        PoleElementDef = ElementProvider.Instance.GetElementDefinition("Pole");
        Instance = this;
        Maxima = new MaximaService(PathToMaxima);
        GetTree().AutoAcceptQuit = false;
        GetTree().QuitOnGoBack = false;
    }

    public override void _Notification(int what)
    {
        if (what != NotificationWMCloseRequest && what != NotificationWMGoBackRequest)
            return;

        SettingsController.Save();
        GetTree().Quit();
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
                ColorJunctions();
                GetViewport().SetInputAsHandled();
                return;
            }
            if (key.Keycode == Key.D)
            {
                foreach (Element e in CircuitManager.Instance.GetElements())
                    if (e.Data.Ports.Count == 2)
                        CircuitManager.Instance.CalculateLoop(e.Data.Ports[0].Id, e.Data.Ports[1].Id);

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
                if (circuitPath == null)
                    SaveFileDialog();
                else
                    save_circuit(circuitPath);
                GetViewport().SetInputAsHandled();
                return;
            }
            if (key.Keycode == Key.N)
            {
                CircuitManager.Instance.LoadCircuit(null);
                GetViewport().SetInputAsHandled();
                return;
            }
            if (key.Keycode == Key.L)
            {
                OpenFileDialog();
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

                CircuitManager.Instance.CreateElement(Toolbar.Instance.SelectedElement, RelativePosition(mouseClicked.Position));

                GetViewport().SetInputAsHandled();
                return;
            }

            // Create Pole
            if (ConnectingWire != null && mouseClicked.ButtonIndex == MouseButton.Left && !mouseClicked.Pressed)
            {
                var createdPole = CircuitManager.Instance.CreateElement(PoleElementDef, SnapToGrid(RelativePosition(mouseClicked.Position)));

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

        if (ConnectingWire != null && ConnectingWire.IsInsideTree() && @event is InputEventMouseMotion mouseMoved)
        {
            ConnectingWire.Points = new Vector2[] { CurrentlyConnecting.Centroid, RelativePosition(mouseMoved.Position) };
        }
    }

    private void ColorJunctions()
    {
        var junctions = CircuitManager.Instance.CalculateNodes();

        foreach (var junction in junctions)
        {
            Color junctionColor = GodotHelpers.RandomColor();
            foreach (string port in junction)
            {
                var conns = CircuitManager.Instance.FindBoundConnections(port);

                foreach (var conn in conns)
                {
                    conn.Line.DefaultColor = junctionColor;
                }
            }
        }

        Logger.Debug($"Generated junctions:\n{string.Join('\n', junctions.ConvertAll<string>(x => string.Join(',', x)))}");
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

    // TODO: Move to other node
    public void OpenFileDialog()
    {
        Node dialoghelper = GetNode("/root/main/DialogHelper");
        dialoghelper.Call("open_file_dialog", SettingsController.Data.LastDialog);
    }

    // TODO: Move to other Node
    public void SaveFileDialog()
    {
        Node dialoghelper = GetNode("/root/main/DialogHelper");
        dialoghelper.Call("save_file_dialog", SettingsController.Data.LastDialog);
    }

    // TODO: Move to other Node
    //Called from DialogHelper.gd
    void load_circuit(string loadPath)
    {
        Logger.Info($"Loading circuit from {loadPath}");

        SettingsController.Data.LastDialog = Path.GetDirectoryName(loadPath);

        circuitPath = loadPath;

        var circuitFile = Godot.FileAccess.Open(loadPath, Godot.FileAccess.ModeFlags.Read);
        var circuitJsonText = circuitFile.GetAsText();
        circuitFile.Close();

        CircuitManager.Instance.LoadCircuit(circuitJsonText);
    }

    // TODO: Move to other Node
    //Called from DialogHelper.gd
    public void save_circuit(string savePath)
    {
        Logger.Info("Saving current Circuit state");

        circuitPath = savePath;
        SettingsController.Data.LastDialog = Path.GetDirectoryName(savePath);

        var circuitFile = Godot.FileAccess.Open(savePath, Godot.FileAccess.ModeFlags.Write);
        var circuitJsonText = CircuitManager.Instance.SaveCircuit();

        circuitFile.StoreString(circuitJsonText);
        circuitFile.Close();

        Logger.Debug($"Saved CircuitData to file {savePath}:\n{circuitJsonText}");
    }
}
