using System;
using Godot;

public partial class ToolbarItem : Button
{
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public ElementDefinition ElementDefinition;
    public Toolbar ParentToolbar;

    private bool IsSelected = false;

    public override void _Ready()
    {
        var texture = GetNode<TextureRect>("MarginContainer/ElementTexture");
        texture.Texture = ElementDefinition.ToolbarTexture;
        Name = $"ToolbarItem: {ElementDefinition.Type}";
    }

    public override void _Pressed()
    {
        GetViewport().SetInputAsHandled();
        if (!ButtonPressed)
        {
            Logger.Debug("User tried to unclick currently selected ToolbarItem");
            ButtonPressed = true;
            return;
        }
        Logger.Debug($"ToolbarItem {ElementDefinition.Type} pressed");
        ParentToolbar.ItemClicked(this);
    }

    public void ToggleSelect(bool selected)
    {
        IsSelected = selected;
        ButtonPressed = selected;
    }
}
