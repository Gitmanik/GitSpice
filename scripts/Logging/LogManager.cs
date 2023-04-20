using Godot;
using NLog;

public partial class LogManager : Node
{
    public override void _EnterTree()
    {
        GD.Print("Configuring NLog");
        ConfigNLog();
    }

    private void ConfigNLog()
    {
        NLog.Config.LoggingConfiguration nlog_config = new NLog.Config.LoggingConfiguration();
        NLog.Targets.FileTarget nlog_logfile = new NLog.Targets.FileTarget("logfile")
        {
            Layout = "${longdate}\t${level:uppercase=true}\t${logger}\t${message:withexception=true}",
            FileName = "Logs/Logs.txt",
            ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
            MaxArchiveDays = 30,
            ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
            ArchiveFileName = "Logs/Logs.{##}.txt",
        };

        NLog.Targets.ColoredConsoleTarget nlog_logconsole = new NLog.Targets.ColoredConsoleTarget("logconsole")
        {
            Layout = "${date:format=HH\\:mm\\:ss} ${level:uppercase=true}\t${logger:long=True}: ${message} ${exception:format=message}"
        };

        nlog_config.AddRule(LogLevel.Debug, LogLevel.Fatal, NLogGodotTarget.GenerateTarget());
        nlog_config.AddRule(LogLevel.Debug, LogLevel.Fatal, nlog_logfile);
        nlog_config.AddRule(LogLevel.Info, LogLevel.Fatal, nlog_logconsole);
        NLog.LogManager.Configuration = nlog_config;
    }
}