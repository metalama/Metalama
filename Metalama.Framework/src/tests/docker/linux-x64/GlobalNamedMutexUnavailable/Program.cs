using System;

namespace TestApp;

/// <summary>
/// The application transformed by <see cref="ReturnZeroAspect"/>. Its only purpose is to give the
/// Metalama pipeline something to transform, so that a successful build proves the pipeline ran.
/// </summary>
public class Program
{
    [ReturnZeroAspect]
    public static int Main( string[] args )
    {
        Console.WriteLine( "FAILURE: The aspect did not work." );

        return 5; // This is overridden by the aspect to return 0.
    }
}
