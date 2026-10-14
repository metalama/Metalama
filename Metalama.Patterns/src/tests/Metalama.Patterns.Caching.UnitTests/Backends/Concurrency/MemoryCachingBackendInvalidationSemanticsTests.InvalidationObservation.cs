// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency
{
    public sealed partial class MemoryCachingBackendInvalidationSemanticsTests
    {
        /// <summary>
        /// The observation that a worker of an invalidation records right after the invalidation has returned.
        /// </summary>
        private sealed class InvalidationObservation
        {
            /// <summary>
            /// Gets or sets a value indicating whether the outer item was in the cache right after the invalidation returned.
            /// </summary>
            /// <remarks>
            /// The initial value is <see langword="true"/>, so that a worker that does not record the observation fails the
            /// assertion. The worker writes the value before it completes, and the test reads it after it has joined the
            /// worker, so no further synchronization is required.
            /// </remarks>
            public bool OuterItemPresentAfterReturn { get; set; } = true;
        }
    }
}
