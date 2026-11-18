// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Backends
{
    internal partial class MemoryCachingBackend
    {
        /// <summary>
        /// The backward dependencies of one dependency key: the keys of the items that declare this dependency key in their
        /// forward dependencies (<see cref="CacheItem.Dependencies"/>). The set is read and changed while it is locked.
        /// </summary>
        private sealed class BackwardDependencySet
        {
            /// <summary>
            /// Gets the keys of the items that depend on the dependency.
            /// </summary>
            public HashSet<string> DependentKeys { get; } = new( StringComparer.Ordinal );

            /// <summary>
            /// Gets or sets a value indicating whether the set has been removed from the index. A removed set is never used
            /// again.
            /// </summary>
            public bool IsRemoved { get; set; }
        }
    }
}
