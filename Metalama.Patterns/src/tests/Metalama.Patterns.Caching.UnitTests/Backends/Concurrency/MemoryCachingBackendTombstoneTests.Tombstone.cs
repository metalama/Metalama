// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency
{
    public sealed partial class MemoryCachingBackendTombstoneTests
    {
        /// <summary>
        /// A replacement value with the same shape as the private <c>RemovedValue</c> record of
        /// <see cref="LayeredCachingBackendEnhancer"/>: a <see cref="MemoryCacheItem"/> without a value and without
        /// dependencies, with a state object of its own, and with a timestamp.
        /// </summary>
        /// <remarks>
        /// The replacement branch of <see cref="MemoryCachingBackend.RemoveItemImpl"/> stores a copy made with a
        /// <c>with</c> expression, which keeps the type and the timestamp. The timestamp therefore identifies the operation
        /// that wrote the stored tombstone.
        /// </remarks>
        private sealed record Tombstone : MemoryCacheItem
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Tombstone"/> class.
            /// </summary>
            /// <param name="timestamp">The timestamp that identifies the operation that writes the tombstone.</param>
            public Tombstone( long timestamp ) : base( null, default, new object() )
            {
                this.Timestamp = timestamp;
            }

            /// <summary>
            /// Gets the timestamp that identifies the operation that writes the tombstone.
            /// </summary>
            public long Timestamp { get; }
        }
    }
}
