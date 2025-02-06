// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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