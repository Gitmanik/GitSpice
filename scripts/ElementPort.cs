using Godot;
using System;

public partial class ElementPort : Button
{
	public PortData Data;

	public Vector2 OffsetPosition => GlobalPosition + (this.GetGlobalRect().Size/2);


    public override void _Pressed()
	{
		UserInputController.Instance.ConnectClicked(this);
	}
}
