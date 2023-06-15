using System.IO;
using Godot;

namespace Gitmanik.Models;

public class Settings
{
    public string LastDialog = Path.GetDirectoryName(Godot.OS.GetExecutablePath());
    public float ElementRotationAmount = 2 * Mathf.Pi / 8f;
    public float ZoomMultiplier = 0.1f;
    public string PathToMaxima = @"C:\maxima-5.46.0\bin\maxima.bat";
    public bool DebugDrawIds = true;
    public float GridSize = 10f;
}