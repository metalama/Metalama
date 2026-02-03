// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Tests.Backends;

public sealed partial class CachingBackendDisposalTests
{
    /// <summary>
    /// A test backend that captures status transitions during disposal.
    /// </summary>
    private sealed class StatusCapturingCachingBackend : CachingBackend
    {
        private readonly List<CachingBackendStatus> _capturedStatuses;

        public StatusCapturingCachingBackend( IServiceProvider serviceProvider, List<CachingBackendStatus> capturedStatuses )
            : base( new MemoryCachingBackendConfiguration { DebugName = "status-capturing" }, serviceProvider )
        {
            this._capturedStatuses = capturedStatuses;
        }

        protected override void SetItemCore( string key, CacheItem item ) { }

        protected override bool ContainsItemCore( string key ) => false;

        protected override CacheItem? GetItemCore( string key, bool includeDependencies ) => null;

        protected override void RemoveItemCore( string key ) { }

        protected override void InvalidateDependencyCore( string key ) { }

        protected override bool ContainsDependencyCore( string key ) => false;

        protected override void ClearCore( ClearCacheOptions options ) { }

        protected override void DisposeCore( bool disposing, CancellationToken cancellationToken )
        {
            // Capture status during disposal
            this._capturedStatuses.Add( this.Status );
            base.DisposeCore( disposing, cancellationToken );
        }

        protected override async ValueTask DisposeAsyncCore( CancellationToken cancellationToken )
        {
            // Capture status during disposal
            this._capturedStatuses.Add( this.Status );
            await base.DisposeAsyncCore( cancellationToken );
        }
    }
}
