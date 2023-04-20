using Godot;
using NLog;
using System;
using System.Collections.Generic;

public partial class UserInputController : Control
{
	private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
	public static UserInputController Instance;

	public CircuitManager CircuitManager = new CircuitManager();

	private ElementPort CurrentConnecting;
	private Line2D CurrentWire;

	private PackedScene ElementScene;
	private Node RootGUINode;

	private static void ConfigNLog()
	{
		NLog.Config.LoggingConfiguration nlog_config = new NLog.Config.LoggingConfiguration();
		NLog.Targets.FileTarget nlog_logfile = new NLog.Targets.FileTarget("logfile")
		{
			Layout = "${longdate}\t${level:uppercase=true}\t${logger}\t${message:withexception=true}",
			FileName = "Logs/Logs.txt",
			ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
			MaxArchiveDays = 30,
			ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
			ArchiveFileName = "Logs/Logs.{##}.txt",
		};

		NLog.Targets.ColoredConsoleTarget nlog_logconsole = new NLog.Targets.ColoredConsoleTarget("logconsole")
		{
			Layout = "${date:format=HH\\:mm\\:ss} ${level:uppercase=true}\t${logger:long=True}: ${message} ${exception:format=message}"
		};

		nlog_config.AddRule(LogLevel.Debug, LogLevel.Fatal, NLogGodotTarget.GenerateTarget());
		nlog_config.AddRule(LogLevel.Debug, LogLevel.Fatal, nlog_logfile);
		nlog_config.AddRule(LogLevel.Info, LogLevel.Fatal, nlog_logconsole);
		NLog.LogManager.Configuration = nlog_config;
	}

    public override void _EnterTree()
    {
		ConfigNLog();
    }

    public override void _Ready()
	{
		ElementScene = GD.Load<PackedScene>("scenes/resistor.tscn");
		RootGUINode = GetNode("/root/main/ElementContainer");

		Instance = this;
	}

    public override void _Input(InputEvent @event)
    {
		if (@event is InputEventKey key && key.Keycode == Key.Backspace)
		{
			Logger.Warn(CircuitManager.SerializeCircuitToJson());
			GetViewport().SetInputAsHandled();
			return;
		}

		if (CurrentWire != null && @event is InputEventMouseButton mouseClicked && mouseClicked.ButtonIndex == MouseButton.Right)
		{
			NLog.LogManager.GetCurrentClassLogger().Info("test");
			ResetConnecting();
			GetViewport().SetInputAsHandled();
			return;
		}
		if (CurrentWire != null && @event is InputEventMouseMotion mouseMoved)
		{
			CurrentWire.Points = new Vector2[] { CurrentConnecting.OffsetPosition, mouseMoved.GlobalPosition};
		}
    }

    private void ResetConnecting()
    {
		CurrentConnecting = null;
		CurrentWire = null;
    }

    public void ConnectClicked(ElementPort clickedPort)
	{	
		if (CurrentConnecting != null)
		{
			if (CircuitManager.Circuit.ConnectionExists(CurrentConnecting.Data.Id, clickedPort.Data.Id))
			{
				CurrentWire.QueueFree();
				ResetConnecting();
				return;
			}
			var conn = CircuitManager.Circuit.ConnectPorts(CurrentConnecting.Data.Id, clickedPort.Data.Id);
			CurrentWire.Points = new Vector2[] { CurrentConnecting.OffsetPosition, clickedPort.OffsetPosition};

			CircuitManager.BindConnection(conn, CurrentConnecting, clickedPort, CurrentWire);
			ResetConnecting();
		}
		else
		{
			CurrentConnecting = clickedPort;
			CurrentWire = new Line2D();
			CurrentWire.Points = new Vector2[] { CurrentConnecting.OffsetPosition + clickedPort.GlobalPosition};
			RootGUINode.AddChild(CurrentWire);
		}
	}


    public override void _UnhandledInput(InputEvent @event)
    {

		if (@event is InputEventMouseButton e && e.ButtonIndex == MouseButton.Left && e.Pressed)
		{
			Logger.Debug("Creating new Element");
			
			//TODO: Move to Element
			var eldata = new ElementData("Resistor");
			eldata.Data = new Dictionary<string, string>();
			eldata.Data.Add("testkey", "testval");

			eldata.Ports = new List<PortData>();
			eldata.Ports.Add(new PortData());
			eldata.Ports.Add(new PortData());
			//

			var newElement = ElementScene.Instantiate<Control>();
			CircuitManager.CreateElement(eldata);
			CircuitManager.BindElement(eldata, newElement as Element);

			newElement.Position = e.Position;
			RootGUINode.AddChild(newElement);
		}
    }

    public void MoveElement(Element element, InputEventMouseMotion e)
    {
		element.Position = e.Position;

		foreach (ElementPort port in element.Ports)
		{
			List<CircuitManager.BoundConnection> connections = CircuitManager.FindBoundConnections(port.Data.Id);
			foreach (var connection in connections)
			{
				connection.Line.Points = new Vector2 []{connection.Port1.OffsetPosition, connection.Port2.OffsetPosition};
			}	
		}

		GetViewport().SetInputAsHandled();
    }
}
