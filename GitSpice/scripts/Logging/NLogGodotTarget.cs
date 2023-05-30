using Godot;
using NLog;
using NLog.Targets;

namespace Gitmanik.Logging.Godot;

[Target("Godot")]
public sealed class NLogGodotTarget : TargetWithLayout
{
    public static NLogGodotTarget GenerateTarget()
    {
        return new NLogGodotTarget()
        {
            Layout = @"[i]${callsite}:${callsite-linenumber}[/i] ${level}: ${message} ${exception}"
        };
    }
    protected override void Write(LogEventInfo logEvent)
    {
        GD.PrintRich(Layout.Render(logEvent));
    }
}