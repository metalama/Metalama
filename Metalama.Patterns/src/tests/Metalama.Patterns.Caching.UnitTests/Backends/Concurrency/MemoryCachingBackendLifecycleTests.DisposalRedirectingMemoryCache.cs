// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.Extensions.Caching.Memory;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency
{
    public sealed partial class MemoryCachingBackendLifecycleTests
    {
        /// <summary>
        /// An <see cref="IMemoryCache"/> that forwards every call to an <see cref="InterceptingMemoryCache"/>, and that
        /// disposes the memory cache under the <see cref="InterceptingMemoryCache"/> directly when it is disposed.
        /// </summary>
        /// <remarks>
        /// <see cref="InterceptingMemoryCache.Dispose"/> releases every armed gate before it disposes its inner cache. A
        /// thread that is blocked at a gate would then resume at the same time as the inner cache is disposed, and the
        /// result would depend on which of the two steps runs first. When the backend disposes this class instead, the
        /// memory cache is disposed while the gates stay armed, and the test decides when the blocked thread resumes.
        /// </remarks>
        private sealed class DisposalRedirectingMemoryCache : IMemoryCache
        {
            private readonly InterceptingMemoryCache _interceptingCache;
            private readonly IMemoryCache _underlyingCache;

            /// <summary>
            /// Initializes a new instance of the <see cref="DisposalRedirectingMemoryCache"/> class.
            /// </summary>
            /// <param name="interceptingCache">The cache to which every call except the disposal is forwarded.</param>
            /// <param name="underlyingCache">The cache that <paramref name="interceptingCache"/> wraps, which this class disposes.</param>
            public DisposalRedirectingMemoryCache( InterceptingMemoryCache interceptingCache, IMemoryCache underlyingCache )
            {
                this._interceptingCache = interceptingCache;
                this._underlyingCache = underlyingCache;
            }

            /// <inheritdoc />
            public bool TryGetValue( object key, out object? value ) => this._interceptingCache.TryGetValue( key, out value );

            /// <inheritdoc />
            public ICacheEntry CreateEntry( object key ) => this._interceptingCache.CreateEntry( key );

            /// <inheritdoc />
            public void Remove( object key ) => this._interceptingCache.Remove( key );

            /// <summary>
            /// Disposes the underlying memory cache without releasing the gates of the <see cref="InterceptingMemoryCache"/>.
            /// </summary>
            public void Dispose() => this._underlyingCache.Dispose();
        }
    }
}
