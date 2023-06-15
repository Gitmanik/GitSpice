using System.IO;
using Gitmanik.Controllers.Logging.Godot;
using NLog;

namespace Gitmanik.Controllers.Logging;

public static class LogController
{
    public static void Configure(string logPath)
    {
        NLog.Config.LoggingConfiguration nlogConfig = new NLog.Config.LoggingConfiguration();

        if (logPath != null)
        {
            NLog.Targets.FileTarget nlogFile = new NLog.Targets.FileTarget()
            {
                Layout = "${longdate}\t${level:uppercase=true}\t${logger}\t${message:withexception=true}",
                FileName = Path.Combine(logPath, "NLog.txt"),
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                MaxArchiveDays = 30,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
                ArchiveFileName = Path.Combine(logPath, "NLog.txt"),
            };
            nlogConfig.AddRule(LogLevel.Trace, LogLevel.Fatal, nlogFile);
        }

        NLog.Targets.ColoredConsoleTarget nlogConsole = new NLog.Targets.ColoredConsoleTarget()
        {
            Layout = "${date:format=HH\\:mm\\:ss} ${level:uppercase=true}\t${logger:long=True}: ${message} ${exception:format=message}"
        };
        nlogConfig.AddRule(LogLevel.Info, LogLevel.Fatal, nlogConsole);

        nlogConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, NLogGodotTarget.GenerateTarget());
        NLog.LogManager.Configuration = nlogConfig;
    }
}