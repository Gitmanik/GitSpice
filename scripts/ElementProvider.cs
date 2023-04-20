using System.Collections.Generic;
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
            ElementDefinition elementDefinition = ResourceLoader.Load<ElementDefinition>(path);
            Elements.Add(elementDefinition.Type, elementDefinition);
        }
    }

    public ElementDefinition GetElement(string Type) => Elements[Type];

    public ElementData NewElementData(string Type)
    {
        var elementDef = GetElement(Type);

        ElementData elementData = new ElementData(Type);

        elementData.Ports = new List<PortData>();
        elementData.Data = new Dictionary<string, string>();

        for (int ctr = 0; ctr < elementDef.PortCount; ctr++)
            elementData.Ports.Add(new PortData());

        //TODO: Cache
        foreach (KeyValuePair<string, string> godotKVP in elementDef.Data)
            elementData.Data.Add(godotKVP.Key, godotKVP.Value);

        return elementData;
    }
}