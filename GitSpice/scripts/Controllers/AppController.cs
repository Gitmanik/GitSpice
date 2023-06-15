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
    public static MaximaService Maxima { get; private set; }

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
        Maxima = new MaximaService(Settings.Data.PathToMaxima);
    }

    public override void _Notification(int what)
    {
        if (what != NotificationWMCloseRequest)
            return;

        Settings.SaveData();
        GetTree().Quit();
    }
}