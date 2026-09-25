// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.Extensions.Caching.Memory;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency
{
    internal sealed partial class InterceptingMemoryCache
    {
        /// <summary>
        /// A post-eviction callback invocation captured by <see cref="InterceptingMemoryCache.DeferEvictionCallbacks"/>.
        /// </summary>
        private sealed class CapturedEvictionCallback
        {
            private readonly PostEvictionDelegate _callback;
            private readonly object _key;
            private readonly object? _value;
            private readonly object? _state;

            public CapturedEvictionCallback( PostEvictionDelegate callback, object key, object? value, EvictionReason reason, object? state )
            {
                this._callback = callback;
                this._key = key;
                this._value = value;
                this.Reason = reason;
                this._state = state;
            }

            /// <summary>
            /// Gets the cache key of the evicted entry.
            /// </summary>
            public string Key => this._key as string ?? this._key.ToString() ?? string.Empty;

            /// <summary>
            /// Gets the reason of the eviction.
            /// </summary>
            public EvictionReason Reason { get; }

            /// <summary>
            /// Gets or sets a value indicating whether the callback has run.
            /// </summary>
            public bool HasRun { get; set; }

            /// <summary>
            /// Runs the callback on the current thread.
            /// </summary>
            public void Run() => this._callback( this._key, this._value, this.Reason, this._state );
        }
    }
}
