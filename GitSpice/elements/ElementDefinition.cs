using Godot;
using Godot.Collections;

public partial class ElementDefinition : Resource
{
    [Export]
    public string Type;

    [Export]
    public PackedScene Scene;

    [Export]
    public Dictionary<string, string> Data;

    [Export]
    public int PortCount;

    [Export]
    public Texture2D ToolbarTexture;

    [Export]
    public bool ShowInToolbar = true;

    [Export]
    public bool AllowRotation = true;
}
