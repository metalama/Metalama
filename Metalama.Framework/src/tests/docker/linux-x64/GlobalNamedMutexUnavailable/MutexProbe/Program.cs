using System;
using System.Threading;

namespace MutexProbe;

/// <summary>
/// Creates a global named mutex the same way <c>Metalama.Backstage.Utilities.MutexHelper</c> does,
/// and reports the outcome.
/// </summary>
/// <remarks>
/// This program deliberately does not reference Metalama. It establishes whether the
/// environment prepared by <c>test.ps1</c> actually prevents the .NET runtime from creating a
/// global named mutex. Without it, a passing test could mean either that Metalama tolerates the
/// condition or that the condition was never reproduced in the first place.
/// </remarks>
public static class Program
{
    public static int Main( string[] args )
    {
        var name = args.Length > 0 ? args[0] : @"Global\Metalama.Probe";

        try
        {
            Mutex mutex;

            if ( Mutex.TryOpenExisting( name, out var existing ) )
            {
                mutex = existing;
            }
            else
            {
                mutex = new Mutex( false, name );
            }

            using ( mutex )
            {
                Console.WriteLine( $"MUTEX_OK {name}" );
            }

            return 0;
        }
        catch ( Exception e )
        {
            Console.WriteLine( $"MUTEX_FAIL {e.GetType().FullName}" );
            Console.WriteLine( $"  HResult: 0x{e.HResult:X8}" );
            Console.WriteLine( $"  Message: {e.Message}" );

            return 1;
        }
    }
}
