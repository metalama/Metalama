// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Tests.RedisServer;

namespace Metalama.Patterns.Caching.Tests;

#pragma warning disable CA2201

/// <summary>
/// Maintains a single instance of a Redis server that's disposed only when all tests finish executing. That way, we don't spend time starting the server
/// again at the beginning of each test.
/// </summary>
public sealed class RedisAssemblyFixture : IDisposable
{
    void IDisposable.Dispose() => this.RedisCleanup();

    private readonly object _lock = new();
    private RedisTestInstance? _testInstance;

    /// <summary>
    /// Gets a reference to the shared Redis server running on this computer. The first time this method is called, the server process is launched first. 
    /// </summary>
    public RedisTestInstance TestInstance
    {
        get
        {
            lock ( this._lock )
            {
                this._testInstance ??= new RedisTestInstance( nameof(RedisAssemblyFixture) );

                if ( this._testInstance.IsDisposed )
                {
                    throw new ObjectDisposedException( nameof(RedisAssemblyFixture) + "." + nameof(this.TestInstance) );
                }
            }

            // [Porting] Not fixing, can't be certain of original intent.
            // ReSharper disable once InconsistentlySynchronizedField
            return this._testInstance;
        }
    }

    /// <summary>
    /// Kills the shared Redis server (the redis-test-8fgd53f4sgd.exe process).
    /// </summary>
    private void RedisCleanup()
    {
        lock ( this._lock )
        {
            if ( this._testInstance is { IsDisposed: false } )
            {
                this._testInstance.Dispose();
            }
        }

        foreach ( var instanceWr in RedisTestInstance.Instances )
        {
            if ( instanceWr.TryGetTarget( out var instance ) && !instance.IsDisposed )
            {
                // The exception is silently ignored when the tests are run with Rider's Live Test Runner.
                // If that's not the expected behavior, we should probably report it...
                throw new Exception( $"RedisTestInstance '{instance.Name}' was not disposed." );
            }
        }
    }
}