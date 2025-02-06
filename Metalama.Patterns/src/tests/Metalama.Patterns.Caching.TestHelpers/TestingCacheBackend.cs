// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using CacheItem = Metalama.Patterns.Caching.Implementation.CacheItem;

namespace Metalama.Patterns.Caching.TestHelpers
{
    public sealed class TestingCacheBackend : CachingBackend
    {
        private readonly string _name;
        private readonly CachingBackend _backend;

        // TODO: This pattern might be in a separate class

        private int _actualSetCount;
        private int _actualContainsKeyCount;
        private int _actualGetCount;
        private int _actualRemoveCount;
        private int _actualInvalidateCount;

        public int ExpectedSetCount { get; set; }

        public int ExpectedContainsKeyCount { get; set; }

        public int ExpectedGetCount { get; set; }

        public int ExpectedRemoveCount { get; set; }

        public int ExpectedInvalidateCount { get; set; }

        public string? LastCachedKey { get; private set; }

        public CacheItem? LastCachedItem { get; private set; }

        public event CacheItemSetEventHandler? ItemSet;

        protected override CachingBackendFeatures CreateFeatures() => this._backend.SupportedFeatures;

        public TestingCacheBackend( string name, IServiceProvider? serviceProvider = null ) : base( serviceProvider: serviceProvider )
        {
            this._name = name;
            this.ResetExpectations();
            this._backend = MemoryCacheFactory.CreateBackend( serviceProvider );
            this._backend.ItemRemoved += this.OnItemRemoved;
            this._backend.DependencyInvalidated += this.OnDependencyInvalidated;
        }

        private void OnDependencyInvalidated( object? sender, CacheDependencyInvalidatedEventArgs args ) => this.OnDependencyInvalidated( args );

        private void OnItemRemoved( object? sender, CacheItemRemovedEventArgs args ) => this.OnItemRemoved( args );

        private void ResetExpectations()
        {
            this.ExpectedSetCount = 0;
            this.ExpectedContainsKeyCount = 0;
            this.ExpectedGetCount = 0;
            this.ExpectedRemoveCount = 0;
            this.ExpectedInvalidateCount = 0;
        }

        public void AssertAndReset( string locationDescription )
        {
            AssertEx.Equal(
                this.ExpectedSetCount,
                this._actualSetCount,
                $"{locationDescription}: The set operation was not called as many times as expected." );

            AssertEx.Equal(
                this.ExpectedContainsKeyCount,
                this._actualContainsKeyCount,
                $"{locationDescription}: The contains key operation was not called as many times as expected." );

            AssertEx.Equal(
                this.ExpectedGetCount,
                this._actualGetCount,
                $"{locationDescription}: The get operation was not called as many times as expected." );

            AssertEx.Equal(
                this.ExpectedRemoveCount,
                this._actualRemoveCount,
                $"{locationDescription}: The remove operation was not called as many times as expected." );

            AssertEx.Equal(
                this.ExpectedInvalidateCount,
                this._actualInvalidateCount,
                $"{locationDescription}: The invalidate object operation was not called as many times as expected." );

            this._actualSetCount = 0;
            this._actualContainsKeyCount = 0;
            this._actualGetCount = 0;
            this._actualRemoveCount = 0;
            this._actualInvalidateCount = 0;
            this.ResetExpectations();
        }

        protected override void SetItemCore( string key, CacheItem item )
        {
            ++this._actualSetCount;
            this._backend.SetItem( key, item );
            this.LastCachedItem = item;
            this.LastCachedKey = key;
            this.ItemSet?.Invoke( this, new CacheItemSetEventArgs( key, item, null ) );
        }

        protected override async ValueTask SetItemAsyncCore( string key, CacheItem item, CancellationToken cancellationToken )
        {
            ++this._actualSetCount;
            await this._backend.SetItemAsync( key, item, cancellationToken );
            this.LastCachedItem = item;
            this.LastCachedKey = key;
            this.ItemSet?.Invoke( this, new CacheItemSetEventArgs( key, item, null ) );
        }

        protected override bool ContainsItemCore( string key )
        {
            ++this._actualContainsKeyCount;

            return this._backend.ContainsItem( key );
        }

        protected override async ValueTask<bool> ContainsItemAsyncCore( string key, CancellationToken cancellationToken )
        {
            ++this._actualContainsKeyCount;

            return await this._backend.ContainsItemAsync( key, cancellationToken );
        }

        protected override CacheItem? GetItemCore( string key, bool includeDependencies )
        {
            ++this._actualGetCount;

            return this._backend.GetItem( key, includeDependencies );
        }

        protected override async ValueTask<CacheItem?> GetItemAsyncCore( string key, bool includeDependencies, CancellationToken cancellationToken )
        {
            ++this._actualGetCount;

            return await this._backend.GetItemAsync( key, includeDependencies, cancellationToken );
        }

        protected override void InvalidateDependencyCore( string key )
        {
            ++this._actualInvalidateCount;
            this._backend.InvalidateDependency( key );
        }

        protected override async ValueTask InvalidateDependencyAsyncCore( string key, CancellationToken cancellationToken )
        {
            ++this._actualInvalidateCount;
            await this._backend.InvalidateDependencyAsync( key, cancellationToken );
        }

        protected override bool ContainsDependencyCore( string key )
        {
            return this._backend.ContainsDependency( key );
        }

        protected override ValueTask<bool> ContainsDependencyAsyncCore( string key, CancellationToken cancellationToken )
        {
            return this._backend.ContainsDependencyAsync( key, cancellationToken );
        }

        protected override void DisposeCore( bool disposing, CancellationToken cancellationToken )
        {
            // ReSharper disable once MethodSupportsCancellation
            this._backend.Dispose();
            AssertEx.Equal( 0, this.BackgroundTaskExceptions, "Exceptions occurred when executing background tasks." );
        }

        protected override async ValueTask DisposeAsyncCore( CancellationToken cancellationToken )
        {
            await this._backend.DisposeAsync( cancellationToken );
            AssertEx.Equal( 0, this.BackgroundTaskExceptions, "Exceptions occurred when executing background tasks." );
        }

        protected override void ClearCore( ClearCacheOptions options )
        {
            this._backend.Clear();
        }

        protected override ValueTask ClearAsyncCore( ClearCacheOptions options, CancellationToken cancellationToken )
        {
            return this._backend.ClearAsync( options, cancellationToken );
        }

        protected override void RemoveItemCore( string key )
        {
            ++this._actualRemoveCount;
            this._backend.RemoveItem( key );
        }

        protected override async ValueTask RemoveItemAsyncCore( string key, CancellationToken cancellationToken )
        {
            ++this._actualRemoveCount;
            await this._backend.RemoveItemAsync( key, cancellationToken );
        }

        public override string ToString() => $"Backend {this._name}";
    }
}