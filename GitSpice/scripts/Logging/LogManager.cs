using System.Collections.Generic;
using Godot;
using NLog;

namespace Gitmanik.Logging.Godot
{
    public partial class LogManager : Node
    {
        public override void _EnterTree()
        {
            GD.Print("Gitmanik LogManager: Configuring NLog");
            ConfigNLog();
        }

        private void ConfigNLog()
        {
            NLog.Config.LoggingConfiguration nlog_config = new NLog.Config.LoggingConfiguration();
            NLog.Targets.FileTarget nlog_logfile = new NLog.Targets.FileTarget()
            {
                Layout = "${longdate}\t${level:uppercase=true}\t${logger}\t${message:withexception=true}",
                FileName = "Logs/Logs.txt",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                MaxArchiveDays = 30,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
                ArchiveFileName = "Logs/Logs.{##}.txt",
            };

            NLog.Targets.ColoredConsoleTarget nlog_logconsole = new NLog.Targets.ColoredConsoleTarget()
            {
                Layout = "${date:format=HH\\:mm\\:ss} ${level:uppercase=true}\t${logger:long=True}: ${message} ${exception:format=message}"
            };

            NLog.Loki.LokiTarget nlog_loki = new NLog.Loki.LokiTarget()
            {
                Endpoint = "http://localhost:3100",
                OrderWrites = false,
                Username = "gitspice",
                Password = "0Vt7YelNav96FpK8SMz19GzM",
                Layout = "${date:format=HH\\:mm\\:ss} ${level:uppercase=true}\t${logger:long=True}: ${message} ${exception:format=message}"
            };

            nlog_loki.Labels.Add(new NLog.Loki.LokiTargetLabel() { Name = "Gitcorp", Layout = "GitSpice" });

            nlog_config.AddRule(LogLevel.Debug, LogLevel.Fatal, nlog_loki);
            nlog_config.AddRule(LogLevel.Debug, LogLevel.Fatal, NLogGodotTarget.GenerateTarget());
            nlog_config.AddRule(LogLevel.Debug, LogLevel.Fatal, nlog_logfile);
            nlog_config.AddRule(LogLevel.Info, LogLevel.Fatal, nlog_logconsole);
            NLog.LogManager.Configuration = nlog_config;
        }
    }
}