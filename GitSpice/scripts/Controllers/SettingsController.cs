using System;
using System.IO;
using System.Text.Json;
namespace Gitmanik.Controllers;

public class SettingsController<T> where T : class, new()
{
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public T Data;

    private string DataPath;

    public SettingsController(string dataPath)
    {
        DataPath = dataPath;
        if (File.Exists(DataPath))
        {
            Logger.Debug("Loading Data file");
            Data = JsonSerializer.Deserialize<T>(File.ReadAllText(DataPath), options: new System.Text.Json.JsonSerializerOptions() { IncludeFields = true });
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

    private void CreateNewData()
    {
        Data = new T();
        SaveData();
    }

    public void SaveData()
    {
        Logger.Debug("Saving data");
        try
        {
            File.WriteAllText(DataPath, JsonSerializer.Serialize(Data, options: new System.Text.Json.JsonSerializerOptions() { IncludeFields = true }));
        }
        catch (Exception e)
        {
            Logger.Error($"Exception while saving data:\n{e}");
        }
    }
}