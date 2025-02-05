using System;
using System.Threading.Tasks;

Console.WriteLine("TopLevelStatement1");

await Task.Yield();

Console.WriteLine("TopLevelStatement2");