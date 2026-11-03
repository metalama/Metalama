// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Tests.Backends;

public sealed partial class CachingBackendDisposalTests
{
    /// <summary>
    /// A test backend whose disposal waits for a callback that runs in a background task and disposes the backend.
    /// </summary>
    /// <remarks>
    /// This backend models a backend that processes messages in a background receive loop, and whose disposal waits for the receive loop.
    /// When the callback disposes the backend, <see cref="ShouldWaitForDisposalInProgress"/> returns <c>false</c>, so the callback does not
    /// wait for the disposal in progress, which waits for the callback.
    /// </remarks>
    private sealed class CallbackDisposingCachingBackend : CachingBackend
    {
        private readonly AsyncLocal<bool> _isInCallback = new();

        public CallbackDisposingCachingBackend( IServiceProvider serviceProvider )
            : base( new MemoryCachingBackendConfiguration { DebugName = "callback-disposing" }, serviceProvider ) { }

        /// <summary>
        /// Gets the status of the backend when the disposal called from the callback returned.
        /// </summary>
        public CachingBackendStatus? StatusAfterCallbackDisposal { get; private set; }

        protected override bool ShouldWaitForDisposalInProgress() => !this._isInCallback.Value;

        protected override void SetItemCore( string key, CacheItem item ) { }

        protected override bool ContainsItemCore( string key ) => false;

        protected override CacheItem? GetItemCore( string key, bool includeDependencies ) => null;

        protected override void RemoveItemCore( string key ) { }

        protected override void InvalidateDependencyCore( string key ) { }

        protected override bool ContainsDependencyCore( string key ) => false;

        protected override void ClearCore( ClearCacheOptions options ) { }

        protected override void DisposeCore( bool disposing, CancellationToken cancellationToken )
        {
            Task.Run(
                    () =>
                    {
                        this._isInCallback.Value = true;
                        this.Dispose( cancellationToken );
                        this.StatusAfterCallbackDisposal = this.Status;
                    },
                    CancellationToken.None )
                .Wait( cancellationToken );

            base.DisposeCore( disposing, cancellationToken );
        }

        protected override async ValueTask DisposeAsyncCore( CancellationToken cancellationToken )
        {
            await Task.Run(
                    async () =>
                    {
                        this._isInCallback.Value = true;
                        await this.DisposeAsync( cancellationToken );
                        this.StatusAfterCallbackDisposal = this.Status;
                    },
                    CancellationToken.None )
                .WithCancellation( cancellationToken );

            await base.DisposeAsyncCore( cancellationToken );
        }
    }
}
