// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Backends
{
    internal partial class MemoryCachingBackend
    {
        /// <summary>
        /// The features of <see cref="MemoryCachingBackend"/>.
        /// </summary>
        private sealed class Features : CachingBackendFeatures
        {
            /// <summary>
            /// Gets a value indicating whether the backend supports <see cref="CachingBackend.Clear"/>. The backend
            /// clears its items one key at a time, so the feature does not depend on the type of the
            /// <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>.
            /// </summary>
            public override bool Clear => true;

            /// <summary>
            /// Gets a value indicating whether the backend supports <see cref="CachingBackend.SetItemIfNotInvalidated"/>.
            /// The backend records the invalidations of its keys in memory, so it detects the invalidations that this
            /// instance performs.
            /// </summary>
            public override bool SetItemIfNotInvalidated => true;
        }
    }
}
