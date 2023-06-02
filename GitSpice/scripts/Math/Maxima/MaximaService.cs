using System;
using System.Diagnostics;
using System.IO;

namespace Gitmanik.Math;

public class MaximaService
{
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private Process MaximaProcess = null;
    private string PathToMaxima;

    public MaximaService(string pathToMaxima)
    {
        Logger.Info("Creating MaximaService");
        PathToMaxima = pathToMaxima;
        MaximaProcess = SpawnMaxima();
    }

    private Process SpawnMaxima()
    {
        Logger.Debug("Spawning new Maxima process");
        if (MaximaProcess != null)
        {
            Logger.Debug("Disposing existing Maxima process");
            MaximaProcess.Dispose();
        }
        var startInfo = new ProcessStartInfo(PathToMaxima, @"-eval ""(cl-user::run)"" -f -- -very-quiet")
        {
            WorkingDirectory = Path.GetDirectoryName(PathToMaxima),
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        return Process.Start(startInfo);
    }

    public string Evaluate(string expr)
    {
        expr = expr.Trim(';');

        //grind() trims output and prints $ at the end of result
        expr = $"grind({expr});";
        Logger.Debug($"Writing {expr} to Maxima");
        MaximaProcess.StandardInput.WriteLine(expr);

        var result = "";
        while (!result.EndsWith('$'))
        {
            result += MaximaProcess.StandardOutput.ReadLine();

            if (result.Contains("incorrect"))
            {
                result += MaximaProcess.StandardOutput.ReadLine();
                result += MaximaProcess.StandardOutput.ReadLine();
                Logger.Error($"Maxima error!\n{result}");
                throw new Exception($"Maxima Error: {result}");
            }
        }
        Logger.Info(result);
        MaximaProcess.StandardOutput.ReadLine(); //Removes 'done'

        return result.TrimEnd('$');
    }
}