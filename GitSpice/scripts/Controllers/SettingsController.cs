using System;
using System.IO;
using System.Text.Json;

namespace Gitmanik.Controllers;

public class SettingsController
{
    public class Settings
    {
        public string LastDialog = Path.GetDirectoryName(Godot.OS.GetExecutablePath());
    }

    public static Settings Data { get; private set; }

    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private const string DataPath = "user://settings.json";
    private string GlobalPath => Godot.ProjectSettings.GlobalizePath(DataPath);

    private static SettingsController _Instance;
    private static SettingsController Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = new SettingsController();
            }
            return _Instance;
        }
        set
        {
            _Instance = value;
        }
    }

    private SettingsController()
    {
        Instance = this;

        if (File.Exists(GlobalPath))
        {
            Data = JsonSerializer.Deserialize<Settings>(File.ReadAllText(GlobalPath));
            if (Data == null)
                CreateNewData();
        }
        else
        {
            CreateNewData();
        }
    }

    private void CreateNewData()
    {
        Data = new Settings();
        SaveData();
    }

    private void SaveData()
    {
        try
        {
            File.WriteAllText(GlobalPath, JsonSerializer.Serialize(Data));
        }
        catch (Exception e)
        {
            Logger.Error($"Exceptionm while saving data:\n{e}");
        }
    }

    public static void Save()
    {
        Instance.SaveData();
    }
}