using Godot;

public partial class ElementPort : Button
{
    public PortData Data;

    public Element ParentElement;

    // public Vector2 MiddlePosition => ParentElement.GlobalPosition + OffsetPosition;

    public Vector2 OffsetPosition => GlobalPosition + (this.GetGlobalRect().Size / 2);


    public override void _Pressed()
    {
        UserInputController.Instance.ConnectClicked(this);
    }
}
