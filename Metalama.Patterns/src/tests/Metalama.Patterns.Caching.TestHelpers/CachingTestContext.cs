// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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