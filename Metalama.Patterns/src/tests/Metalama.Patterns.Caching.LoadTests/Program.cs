// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.LoadTests.Tests;

namespace Metalama.Patterns.Caching.LoadTests;

internal static class Program
{
    private static async Task Main()
    {
        Console.WriteLine( "create" );

        var configuration =
            new RedisLoadTestConfiguration()
            {
                CollectorsCount = 0,
                ClientsCount = 1,
                ValueKeyLength = new Interval( 20, 30 ),
                ValueKeysCount = 100,
                ValueKeyExpiry = new Interval( 5, 15 ),
                ValueLength = new Interval( 50, 100 ),
                DependencyKeyLength = new Interval( 20, 30 ),
                DependencyKeysCount = 10000,
                DependenciesPerValueCount = new Interval( 0, 3 ),
                ValuesPerSharedDependency = new Interval( 0, 5 )
            };

        // TimeSpan testDuration = TimeSpan.FromHours( 1 );
        var duration = TimeSpan.FromSeconds( 30 );

        var test = new RedisLoadTest();

        await test.TestAsync( configuration, duration );
    }
}