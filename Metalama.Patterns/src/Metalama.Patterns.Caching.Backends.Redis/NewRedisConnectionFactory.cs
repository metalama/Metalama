// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace;
using Flashtrace.Loggers;
using StackExchange.Redis;

namespace Metalama.Patterns.Caching.Backends.Redis;

internal sealed class NewRedisConnectionFactory( ConfigurationOptions options ) : IRedisConnectionFactory
{
    public IConnectionMultiplexer GetConnection( IServiceProvider? serviceProvider )
    {
        var textWriter = serviceProvider != null
            ? new FlashTraceTextWriter( serviceProvider.GetFlashtraceSource( "Redis", FlashtraceRole.Caching ).Debug )
            : null;

        return Task.Run( () => ConnectionMultiplexer.ConnectAsync( options, textWriter ) ).Result;
    }

    public async Task<IConnectionMultiplexer> GetConnectionAsync(
        IServiceProvider? serviceProvider,
        bool logRedisConnection,
        CancellationToken cancellationToken )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var textWriter = serviceProvider != null && logRedisConnection
            ? new FlashTraceTextWriter( serviceProvider.GetFlashtraceSource( "Redis", FlashtraceRole.Caching ).Debug )
            : null;

        return await ConnectionMultiplexer.ConnectAsync( options, textWriter );
    }
}