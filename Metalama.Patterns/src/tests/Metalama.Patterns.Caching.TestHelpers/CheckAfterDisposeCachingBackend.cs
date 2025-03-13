// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using Xunit;

namespace Metalama.Patterns.Caching.TestHelpers;

public class CheckAfterDisposeCachingBackend : CachingBackendEnhancer
{
    public CheckAfterDisposeCachingBackend( CachingBackend underlyingBackend ) : base( underlyingBackend ) { }

    protected override void DisposeCore( bool disposing, CancellationToken cancellationToken )
    {
        base.DisposeCore( disposing, cancellationToken );
        Assert.Equal( 0, this.BackgroundTaskExceptions );
    }

    protected override async ValueTask DisposeAsyncCore( CancellationToken cancellationToken )
    {
        await base.DisposeAsyncCore( cancellationToken );
        Assert.Equal( 0, this.BackgroundTaskExceptions );
    }
}