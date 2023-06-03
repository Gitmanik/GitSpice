using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace Gitmanik.Controllers;

public partial class SettingsController : Node
{
    public class Settings
    {
        public string LastDialog = Path.GetDirectoryName(Godot.OS.GetExecutablePath());
    }

    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public Settings Data;

    private const string DataPath = "user://settings.json";
    private string GlobalPath => Godot.ProjectSettings.GlobalizePath(DataPath);
    public static SettingsController Instance;

    public override void _EnterTree()
    {
        Logger.Debug("SettingsController starting");
        Instance = this;

        if (File.Exists(GlobalPath))
        {
            Logger.Debug("Loading Data file");
            Data = JsonSerializer.Deserialize<Settings>(File.ReadAllText(GlobalPath), options: new System.Text.Json.JsonSerializerOptions() { IncludeFields = true });
            if (Data == null)
            {
                Logger.Warn("Data file corrupt!");
                CreateNewData();
            }
        }
        else
        {
            Logger.Debug("Creating new Data");
            CreateNewData();
        }
    }

    public override void _Notification(int what)
    {
        if (what != NotificationWMCloseRequest)
            return;

        SaveData();
    }

    private void CreateNewData()
    {
        Data = new Settings();
        SaveData();
    }

    public void SaveData()
    {
        Logger.Debug("Saving data");
        try
        {
            File.WriteAllText(GlobalPath, JsonSerializer.Serialize(Data, options: new System.Text.Json.JsonSerializerOptions() { IncludeFields = true }));
        }
        catch (Exception e)
        {
            Logger.Error($"Exceptionm while saving data:\n{e}");
        }
    }
}