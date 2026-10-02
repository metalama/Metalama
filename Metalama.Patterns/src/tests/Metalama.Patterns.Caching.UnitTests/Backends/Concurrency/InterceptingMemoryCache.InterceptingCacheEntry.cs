// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using CacheItemPriority = Microsoft.Extensions.Caching.Memory.CacheItemPriority;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency
{
    internal sealed partial class InterceptingMemoryCache
    {
        /// <summary>
        /// An <see cref="ICacheEntry"/> that forwards every member to the entry of the inner cache, and that intercepts
        /// the disposal, which stores the entry.
        /// </summary>
        private sealed class InterceptingCacheEntry : ICacheEntry
        {
            private readonly InterceptingMemoryCache _owner;
            private readonly ICacheEntry _inner;

            public InterceptingCacheEntry( InterceptingMemoryCache owner, ICacheEntry inner )
            {
                this._owner = owner;
                this._inner = inner;
            }

            /// <inheritdoc />
            public object Key => this._inner.Key;

            /// <inheritdoc />
            public object? Value
            {
                get => this._inner.Value;
                set => this._inner.Value = value;
            }

            /// <inheritdoc />
            public DateTimeOffset? AbsoluteExpiration
            {
                get => this._inner.AbsoluteExpiration;
                set => this._inner.AbsoluteExpiration = value;
            }

            /// <inheritdoc />
            public TimeSpan? AbsoluteExpirationRelativeToNow
            {
                get => this._inner.AbsoluteExpirationRelativeToNow;
                set => this._inner.AbsoluteExpirationRelativeToNow = value;
            }

            /// <inheritdoc />
            public TimeSpan? SlidingExpiration
            {
                get => this._inner.SlidingExpiration;
                set => this._inner.SlidingExpiration = value;
            }

            /// <inheritdoc />
            public IList<IChangeToken> ExpirationTokens => this._inner.ExpirationTokens;

            /// <inheritdoc />
            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => this._inner.PostEvictionCallbacks;

            /// <inheritdoc />
            public CacheItemPriority Priority
            {
                get => this._inner.Priority;
                set => this._inner.Priority = value;
            }

            /// <inheritdoc />
            public long? Size
            {
                get => this._inner.Size;
                set => this._inner.Size = value;
            }

            /// <summary>
            /// Replaces the post-eviction callbacks when they must be deferred, then stores the entry in the inner cache.
            /// </summary>
            public void Dispose()
            {
                if ( this._owner.IsDeferred( this._inner.Key ) )
                {
                    foreach ( var registration in this._inner.PostEvictionCallbacks )
                    {
                        var callback = registration.EvictionCallback;

                        if ( callback != null )
                        {
                            registration.EvictionCallback = ( key, value, reason, state ) => this._owner.Capture( callback, key, value, reason, state );
                        }
                    }
                }

                this._owner.Intercept( InterceptedOperation.CommitBefore, this._inner.Key );
                this._inner.Dispose();
                this._owner.Intercept( InterceptedOperation.CommitAfter, this._inner.Key );
            }
        }
    }
}
