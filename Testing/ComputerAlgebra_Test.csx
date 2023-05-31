#r "../GitSpice/ComputerAlgebra.dll"

using ComputerAlgebra;

Variable t = Variable.New("t");
Variable s = Variable.New("s");

Expression ft = "x/2";
Expression fs = ft.LaplaceTransform(t,s);

Console.WriteLine($"aha {fs}");