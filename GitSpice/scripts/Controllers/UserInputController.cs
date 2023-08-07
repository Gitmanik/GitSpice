using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gitmanik.Controllers;
using Gitmanik.Models;
using Gitmanik.Utils;
using Gitmanik.Utils.Extensions;
using Godot;

public partial class UserInputController : Control
{
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static UserInputController Instance;

    private ElementPort CurrentlyConnecting;
    private Line2D ConnectingWire;
    private Control ElementContainerScene;
    public RichTextLabel InfoPanel;

    private ElementDefinition PoleElementDef;
    private string CurrentCircuitPath = null;

    public override void _Ready()
    {
        Instance = this;
        ElementContainerScene = GetNode<Control>(CircuitManager.ElementContainerPath);
        PoleElementDef = ElementProvider.Instance.GetElementDefinition("Pole");
        InfoPanel = GetNode<RichTextLabel>("/root/main/InfoPanel/RichTextLabel");

        // TODO: Remove this
        load_circuit(@"..\Circuits\test.json");
        CircuitManager.Instance.CalculateCurrentSymbols();
    }

    bool mouseClickedBool = false;
    bool mouseDragging = false;
    Vector2 lastMouseDraggingPos;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Echo == false && key.Pressed == true)
        {
            if (key.Keycode == Key.Y)
            {
                CircuitManager.Instance.CalculateCurrentSymbols();
                GetViewport().SetInputAsHandled();
                return;
            }

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
                        CircuitManager.Instance.CalculateLoop(e.Data.Ports[0], e.Data.Ports[1]);

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
                if (Input.IsKeyPressed(Key.Shift) || CurrentCircuitPath == null)
                    SaveFileDialog();
                else
                    save_circuit(CurrentCircuitPath);
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
                Scale += Vector2.One * AppController.Settings.Data.ZoomMultiplier;
                Position -= RelativePosition(mouseClicked.Position) * AppController.Settings.Data.ZoomMultiplier;
                GetViewport().SetInputAsHandled();
                return;
            }
            if (mouseClicked.ButtonIndex == MouseButton.WheelDown && !mouseClicked.Pressed && !Element.IsCurrentlyMoving)
            {
                Scale -= Vector2.One * AppController.Settings.Data.ZoomMultiplier;
                Position += RelativePosition(mouseClicked.Position) * AppController.Settings.Data.ZoomMultiplier;
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

        Logger.Debug($"Generated junctions:\n{string.Join('\n', junctions.ConvertAll<string>(x => x.JoinList()))}");
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
        if (!CircuitManager.Instance.ConnectionExists(CurrentlyConnecting.PortId, clickedPort.PortId))
        {
            CircuitManager.Instance.ConnectPorts(CurrentlyConnecting, clickedPort);
        }
        else
            Logger.Warn($"Tried to create already existing connection! (Port1: {CurrentlyConnecting.PortId}, Port2: {clickedPort.PortId})");

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

    public void ElementSelected(Element element, bool isSelected)
    {
        string infoPanelText = "";

        if (element.Ports.Count == 2)
        {
            var loop = CircuitManager.Instance.CalculateLoop(element.Data.Ports[0], element.Data.Ports[1]);

            CircuitManager.Instance.ColorLoop(loop, isSelected ? GodotHelpers.RandomColor() : Colors.White);

            if (isSelected)
            {
                string secondKirchhoff = CircuitManager.Instance.Calculate2ndKirchhoffLaw(loop);

                Dictionary<string, string> currentSymbols = CircuitManager.Instance.CalculateCurrentSymbols();
                List<ElementData> elements = CircuitManager.Instance.GetAllElementsInLoop(loop);

                var equations = new List<string>();
                equations.Add(secondKirchhoff);

                foreach (var el in elements)
                {
                    foreach (var kvp in el.GetAllValues())
                    {
                        equations.Add($"{kvp.Key}={kvp.Value}");
                    }
                }

                decimal res = AppController.Maxima.SolveLinearSystem(equations)[element.Data.GetVoltage()];
                Logger.Info(res);
                infoPanelText += $"[b]2nd Kirchoff:[/b] {secondKirchhoff}\n";
                infoPanelText += $"[b]Voltage value:[/b] {res}\n";
                infoPanelText += string.Join('\n', element.Data.Data.ToList().ConvertAll(x => $"[b]{x.Key}:[/b] {x.Value}"));
            }
        }

        UserInputController.Instance.InfoPanel.Text = infoPanelText;
    }

    private Vector2 SnapToGrid(Vector2 v)
    {
        v.X = Mathf.Round(v.X / AppController.Settings.Data.GridSize) * AppController.Settings.Data.GridSize;
        v.Y = Mathf.Round(v.Y / AppController.Settings.Data.GridSize) * AppController.Settings.Data.GridSize;
        return v;
    }

    /// <summary>
    /// Calculates position relative to moved ElementContainer (dragged by user)
    /// </summary>
    /// <param name="pos">Screen position</param>
    /// <returns>Relative position</returns>
    private Vector2 RelativePosition(Vector2 pos) => (pos - Position) / Scale;


    #region Dialog handling
    public void OpenFileDialog()
    {
        Node dialoghelper = GetNode("/root/main/DialogHelper");
        dialoghelper.Call("open_file_dialog", AppController.Settings.Data.LastDialog);
    }

    public void SaveFileDialog()
    {
        Node dialoghelper = GetNode("/root/main/DialogHelper");
        dialoghelper.Call("save_file_dialog", AppController.Settings.Data.LastDialog);
    }

    //Called from DialogHelper.gd
    void load_circuit(string loadPath)
    {
        Logger.Info($"Loading circuit from {loadPath}");

        AppController.Settings.Data.LastDialog = Path.GetDirectoryName(loadPath);

        CurrentCircuitPath = loadPath;

        var circuitFile = Godot.FileAccess.Open(loadPath, Godot.FileAccess.ModeFlags.Read);
        var circuitJsonText = circuitFile.GetAsText();
        circuitFile.Close();

        CircuitManager.Instance.LoadCircuit(circuitJsonText);
    }

    //Called from DialogHelper.gd
    public void save_circuit(string savePath)
    {
        Logger.Info("Saving current Circuit state");

        CurrentCircuitPath = savePath;
        AppController.Settings.Data.LastDialog = Path.GetDirectoryName(savePath);

        var circuitFile = Godot.FileAccess.Open(savePath, Godot.FileAccess.ModeFlags.Write);
        var circuitJsonText = CircuitManager.Instance.SaveCircuit();

        circuitFile.StoreString(circuitJsonText);
        circuitFile.Close();

        Logger.Debug($"Saved CircuitData to file {savePath}:\n{circuitJsonText}");
    }
    #endregion
}
