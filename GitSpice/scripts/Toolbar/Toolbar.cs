using Godot;
using System;

public partial class Toolbar : MarginContainer
{
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static Toolbar Instance;

    public ElementDefinition SelectedElement;
    private ToolbarItem SelectedItem;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        Logger.Info("Toolbar setting up");
        var toolbarItemScene = ResourceLoader.Load<PackedScene>("scenes/toolbar_item.tscn");

        var toolbarPanelScene = GetNode<Control>("ToolbarPanel");
        var elementDefs = ElementProvider.Instance.GetElements();
        foreach (var elementDef in elementDefs)
        {
            Logger.Debug($"Registering {elementDef.Type}");
            var toolbarItem = toolbarItemScene.Instantiate<ToolbarItem>();
            toolbarItem.ParentToolbar = this;
            toolbarItem.ElementDefinition = elementDef;

            toolbarPanelScene.AddChild(toolbarItem);
        }
    }

    public void ItemClicked(ToolbarItem clickedItem)
    {
        Logger.Info($"Selected {clickedItem.ElementDefinition.Type}");

        SelectedItem?.ToggleSelect(false);
        SelectedElement = clickedItem.ElementDefinition;

        SelectedItem = clickedItem;
        SelectedItem.ToggleSelect(true);
    }
}
