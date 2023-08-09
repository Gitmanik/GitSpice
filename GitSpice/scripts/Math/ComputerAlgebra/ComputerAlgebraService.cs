using System.Collections.Generic;
using ComputerAlgebra;

namespace Gitmanik.Math;

public class ComputerAlgebraService : IMathService
{
    private readonly static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public void Close()
    {
    }

    public Dictionary<string, decimal> SolveLinearSystem(List<string> equations)
    {
        throw new System.NotImplementedException();

        //TODO: Find a way to solve for all variables
        var res = new Dictionary<string, decimal>();
        var system = new List<Equal>();
        foreach (string eq in equations)
        {
            string x = eq.Substring(0, eq.IndexOf("="));
            string y = eq.Substring(eq.IndexOf("=") + 1);
            Logger.Info($"x:{x} y:{y}");
            system.Add(Equal.New(x, y));
        }

        // Here
        string calcFor = null;

        List<Arrow> solutions = system.Solve(Variable.New(calcFor));
        foreach (Arrow i in solutions)
        {
            Logger.Info($"{i}: {i.Left}, {i.Right}");
            res.Add(i.Left.ToString(), decimal.Parse(i.Right.ToString()));
        }

        return res;
    }
}