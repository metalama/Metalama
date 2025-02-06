// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;
using Xunit;

namespace Metalama.Patterns.Caching.TestHelpers;

public sealed class CachingTestContext<T> : IDisposable, IAsyncDisposable
    where T : CachingBackend
{
    public T Backend { get; }

    internal CachingTestContext( T backend )
    {
        this.Backend = backend;
    }

    public void Dispose()
    {
        CachingService.Default.DefaultBackend.Dispose();
        Assert.Equal( 0, CachingService.Default.DefaultBackend.BackgroundTaskExceptions );
        CachingService.Default = CachingService.CreateUninitialized();
    }

    public async ValueTask DisposeAsync()
    {
        await CachingService.Default.DefaultBackend.DisposeAsync();
        Assert.Equal( 0, CachingService.Default.DefaultBackend.BackgroundTaskExceptions );
        CachingService.Default = CachingService.CreateUninitialized();
    }
}