using System.Collections.Generic;

namespace Gitmanik.Math;

public interface IMathService
{
    Dictionary<string, decimal> SolveLinearSystem(List<string> equations);
    void Close();
}