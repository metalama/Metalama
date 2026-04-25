// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Tests.Backends;

public sealed partial class CachingBackendDisposalTests
{
    /// <summary>
    /// A test backend that blocks during initialization to test cancellation scenarios.
    /// </summary>
    private sealed class SlowInitializingCachingBackend : CachingBackend
    {
        private readonly TaskCompletionSource<bool> _initStarted;
        private readonly TaskCompletionSource<bool> _initComplete;

        public SlowInitializingCachingBackend(
            IServiceProvider serviceProvider,
            TaskCompletionSource<bool> initStarted,
            TaskCompletionSource<bool> initComplete )
            : base( new MemoryCachingBackendConfiguration { DebugName = "slow-init" }, serviceProvider )
        {
            this._initStarted = initStarted;
            this._initComplete = initComplete;
        }

        protected override async Task InitializeCoreAsync( CancellationToken cancellationToken = default )
        {
            this._initStarted.SetResult( true );
            await this._initComplete.Task;
        }

        protected override void SetItemCore( string key, CacheItem item ) { }

        protected override bool ContainsItemCore( string key ) => false;

        protected override CacheItem? GetItemCore( string key, bool includeDependencies ) => null;

        protected override void RemoveItemCore( string key ) { }

        protected override void InvalidateDependencyCore( string key ) { }

        protected override bool ContainsDependencyCore( string key ) => false;

        protected override void ClearCore( ClearCacheOptions options ) { }
    }
}