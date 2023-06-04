using System.Collections.Generic;
using System.Linq;
using Gitmanik.Models;
using Godot;

public partial class ElementProvider : Node
{
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static ElementProvider Instance;
    private const string ElementPath = "res://elements/definitions";
    private Dictionary<string, ElementDefinition> Elements;
    public override void _EnterTree()
    {
        Logger.Debug("ElementProvider starting");
        Instance = this;
        Elements = new Dictionary<string, ElementDefinition>();
        var defDir = DirAccess.Open(ElementPath);

        foreach (var filename in defDir.GetFiles())
        {
            string path = $"{ElementPath}/{filename}";
            Logger.Debug($"Loading Element: {path}");
            if (!filename.EndsWith(".tres"))
            {
                path = path.TrimSuffix(".remap");
                Logger.Debug($".remap file found, trimming: {path}");
            }

            ElementDefinition elementDefinition = ResourceLoader.Load<ElementDefinition>(path);
            elementDefinition.Type = filename.TrimSuffix(".remap").TrimSuffix(".tres");
            Elements.Add(elementDefinition.Type, elementDefinition);
        }
    }

    public ElementDefinition GetElementDefinition(string Type) => Elements[Type];
    public List<ElementDefinition> GetElementDefinitions() => Elements.Values.ToList();

    public ElementData NewElementData(string Type)
    {
        var elementDef = GetElementDefinition(Type);
        return NewElementData(elementDef);
    }

    public ElementData NewElementData(ElementDefinition elementDef)
    {
        ElementData elementData = new ElementData(elementDef.Type);

        elementData.Ports = new List<string>();
        elementData.Data = new Dictionary<string, string>();

        for (int ctr = 0; ctr < elementDef.PortCount; ctr++)
            elementData.Ports.Add(CircuitManager.Instance.NewID());

        foreach (KeyValuePair<string, string> godotKVP in elementDef.Data)
            elementData.Data.Add(godotKVP.Key, godotKVP.Value);

        return elementData;
    }
}