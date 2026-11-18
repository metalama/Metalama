// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Building;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.TestHelpers;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency;

/// <summary>
/// Tests that <see cref="MemoryCachingBackend"/> reports and implements the <see cref="CachingBackendFeatures.Clear"/>
/// feature on an <see cref="IMemoryCache"/> that declares no operation that removes every entry.
/// </summary>
/// <remarks>
/// The backend clears its items one key at a time, with <see cref="IMemoryCache.Remove"/>. The clearing therefore does
/// not depend on the type of the <see cref="IMemoryCache"/>, and the reported feature must not depend on it either.
/// </remarks>
public sealed class MemoryCachingBackendClearFeatureTests
{
    /// <summary>
    /// The key of the item of the test.
    /// </summary>
    private const string _key = "key";

    /// <summary>
    /// The dependency of the item of the test.
    /// </summary>
    private const string _dependency = "dependency";

    /// <summary>
    /// Verifies that the backend reports the <see cref="CachingBackendFeatures.Clear"/> feature, and that
    /// <see cref="CachingBackend.Clear"/> removes the items and their registrations, when the <see cref="IMemoryCache"/>
    /// is neither a <see cref="MemoryCache"/> nor an <see cref="IClearableMemoryCache"/>.
    /// </summary>
    [Fact]
    public void Clear_OnMemoryCacheWithoutClearOperation_IsSupportedAndRemovesItems()
    {
        using var fakes = new FakeCachingServices();
        using var cache = new ForwardingMemoryCache( new MemoryCache( new MemoryCacheOptions() ) );

        using var backend = CachingBackend.Create(
            b => b.Memory( new MemoryCachingBackendConfiguration { DebugName = "test" } ).WithMemoryCache( cache ),
            fakes.ServiceProvider );

        backend.Initialize();
        backend.SetItem( _key, new CacheItem( "value", [_dependency] ) );

        Assert.True( backend.ContainsItem( _key ) && backend.ContainsDependency( _dependency ), "Precondition: the backend did not store the item." );

        Assert.True(
            backend.SupportedFeatures.Clear,
            "The backend does not report the Clear feature, although it clears its items one key at a time on any IMemoryCache." );

        backend.Clear();

        Assert.False( backend.ContainsItem( _key ), "The clearing did not remove the item." );
        Assert.False( backend.ContainsDependency( _dependency ), "The clearing did not remove the registration of the item under its dependency." );
    }

    /// <summary>
    /// An <see cref="IMemoryCache"/> that forwards every call to another <see cref="IMemoryCache"/> and implements no
    /// other interface.
    /// </summary>
    private sealed class ForwardingMemoryCache : IMemoryCache
    {
        private readonly IMemoryCache _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForwardingMemoryCache"/> class.
        /// </summary>
        /// <param name="inner">The cache to which every call is forwarded, and which this instance disposes.</param>
        public ForwardingMemoryCache( IMemoryCache inner )
        {
            this._inner = inner;
        }

        /// <inheritdoc />
        public bool TryGetValue( object key, out object? value ) => this._inner.TryGetValue( key, out value );

        /// <inheritdoc />
        public ICacheEntry CreateEntry( object key ) => this._inner.CreateEntry( key );

        /// <inheritdoc />
        public void Remove( object key ) => this._inner.Remove( key );

        /// <inheritdoc />
        public void Dispose() => this._inner.Dispose();
    }
}
