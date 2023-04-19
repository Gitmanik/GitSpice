using Godot;
using NLog;
using NLog.Targets;

[Target("Godot")]
public sealed class NLogGodotTarget : TargetWithLayout
{
    public static NLogGodotTarget GenerateTarget()
    {
        return new NLogGodotTarget()
        {
            Layout = @"<i>${callsite}:${callsite-linenumber}</i> ${level}: ${message} ${exception}"
        };
    }
    protected override void Write(LogEventInfo logEvent)
    {
        if (logEvent.Level == LogLevel.Warn)
            GD.PushWarning($"{Layout.Render(logEvent)}");
        else if (logEvent.Level == LogLevel.Error || logEvent.Level == LogLevel.Fatal)
            GD.PushError($"{Layout.Render(logEvent)}");
        else
            GD.Print(Layout.Render(logEvent));
    }
}