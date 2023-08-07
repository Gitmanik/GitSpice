using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Gitmanik.Math;

public class MaximaService
{
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private Process MaximaProcess = null;
    private string PathToMaxima;

    private static readonly string[] StartupCommands =
    {
        "[linsolve_params: false, globalsolve: false, numer: true]"
    };

    public MaximaService(string pathToMaxima)
    {
        Logger.Info("Creating MaximaService");
        PathToMaxima = pathToMaxima;
        try
        {
            SpawnMaxima();
        }
        catch (Exception e)
        {
            Logger.Fatal($"Could not spawn Maxima process! {e}");
            return;
        }
    }

    private void SpawnMaxima()
    {
        MaximaProcess = SpawnMaximaProcess();
        foreach (string command in StartupCommands)
            Evaluate(command);
    }

    private Process SpawnMaximaProcess()
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

    public Dictionary<string, decimal> SolveLinearSystem(List<string> equations)
    {
        string command = $"solve([{string.Join(", ", equations)}])";
        string result = Evaluate(command);

        result = result.TrimStart('[').TrimEnd(']');
        Dictionary<string, decimal> roots = new Dictionary<string, decimal>();
        foreach (var res in result.Split(','))
        {
            string[] x = res.Split('=');
            roots.Add(x[0].Trim(), Convert.ToDecimal(x[1].Trim(), System.Globalization.CultureInfo.InvariantCulture));
        }
        return roots;
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

            if (result.Contains("debug"))
            {
                Logger.Error($"Maxima error!\n{result}");
                throw new Exception($"Maxima Error: {result}");
            }
            if (result.Contains("incorrect"))
            {
                result += MaximaProcess.StandardOutput.ReadLine();
                result += MaximaProcess.StandardOutput.ReadLine();
                Logger.Error($"Maxima error!\n{result}");
                throw new Exception($"Maxima Error: {result}");
            }
        }
        result = result.TrimEnd('$');
        Logger.Debug($"Result: {result}");
        MaximaProcess.StandardOutput.ReadLine(); //Removes 'done'

        return result;
    }

    public void KillProcess()
    {
        Logger.Debug("Killing Maxima process");
        MaximaProcess?.Kill();
        Logger.Trace("Killed Maxima process.");
    }
}