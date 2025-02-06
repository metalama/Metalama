// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.LoadTests.Tests;

internal abstract class BaseTestClass<TLoadTestConfigurationT>
    where TLoadTestConfigurationT : LoadTestConfiguration
{
    public virtual Task TestAsync( TLoadTestConfigurationT configuration, TimeSpan duration )
    {
        using ( var test = new LoadTest( configuration ) )
        {
            Console.WriteLine( "test init" );

            test.Initialize( this.CreateCachingBackend );

            Console.WriteLine( "start" );

            test.Start();

            Console.WriteLine( "run" );

            Thread.Sleep( duration );

            Console.WriteLine( "stop" );

            test.Stop();

            test.Report();

            Console.WriteLine( "dispose" );
        }

        Console.WriteLine( "end" );

        return Task.CompletedTask;
    }

    protected abstract CachingBackend CreateCachingBackend();
}