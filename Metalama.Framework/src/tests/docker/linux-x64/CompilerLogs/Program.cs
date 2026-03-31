using System;

namespace TestApp;

public class Program
{
    [ReturnZeroAspect]
    public static int Main(string[] args)
    {
        Console.WriteLine("FAILURE: The aspect did not work.");
        return 5; // This will be overridden by the aspect to return 0
    }
}
