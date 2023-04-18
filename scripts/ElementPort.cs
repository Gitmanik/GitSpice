using Godot;
using System;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public partial class ElementPort : Button
{
	[JsonProperty]
    public string Guid;
	public Vector2 OffsetPosition => GlobalPosition + (this.GetGlobalRect().Size/2);


    public override void _Pressed()
	{
		UserInputController.Instance.ConnectClicked(this);
	}
}
