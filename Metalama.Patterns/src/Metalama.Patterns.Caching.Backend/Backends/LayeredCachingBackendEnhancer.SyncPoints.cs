// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using SharpCrafters.Common.Testing.Hooks;

namespace Metalama.Patterns.Caching.Backends
{
    internal sealed partial class LayeredCachingBackendEnhancer
    {
        /// <summary>
        /// The names of the synchronization points of <see cref="LayeredCachingBackendEnhancer"/>. A test enables a
        /// synchronization point by its name through an <see cref="ITestSynchronizationProvider"/>.
        /// </summary>
        internal static class SyncPoints
        {
            /// <summary>
            /// Reached by a read after it has read an item from the remote cache and before it copies that item into
            /// the local cache.
            /// </summary>
            public const string RemoteItemRead = "LayeredCachingBackendEnhancer.ReadThrough:RemoteItemRead";
        }
    }
}
