public static class ExtensionMethods
{
    public static Godot.Vector2 ToGodot(this System.Numerics.Vector2 v) => new Godot.Vector2(v.X, v.Y);
    public static System.Numerics.Vector2 ToNumerics(this Godot.Vector2 v) => new System.Numerics.Vector2(v.X, v.Y);
}