using System;
using Godot;

namespace Gitmanik.Utils;

public class GodotHelpers
{
    public static byte RandomByte() => Convert.ToByte(GD.Randi() % 256);
    public static Color RandomColor() => Color.Color8(RandomByte(), RandomByte(), RandomByte());
}