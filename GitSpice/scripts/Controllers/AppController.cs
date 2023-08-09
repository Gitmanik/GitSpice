using Gitmanik.Controllers.Logging;
using Gitmanik.Math;
using Gitmanik.Models;
using Godot;

namespace Gitmanik.Controllers;

public partial class AppController : Node
{
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static AppController Instance { get; private set; }
    public static SettingsController<Settings> Settings { get; private set; }
    public static IMathService MathService { get; private set; }

    public override void _EnterTree()
    {
        if (Instance != null)
        {
            Logger.Fatal("Tried to create second AppController!");
            return;
        }
        Instance = this;

        LogController.Configure(ProjectSettings.GlobalizePath("user://NLog/"));
        Settings = new SettingsController<Settings>(ProjectSettings.GlobalizePath("user://settings.json"));
        MathService = new MaximaService(Settings.Data.PathToMaxima);

        GetTree().AutoAcceptQuit = false;
    }

    public override void _Notification(int what)
    {
        if (what != NotificationWMCloseRequest)
            return;

        Logger.Info("Closing app");

        Settings.SaveData();
        MathService.Close();

        GetTree().Quit();
    }
}