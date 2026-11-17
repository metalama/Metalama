// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using SharpCrafters.Common.Testing.Hooks;

namespace Metalama.Patterns.Caching.Backends
{
    internal partial class MemoryCachingBackend
    {
        /// <summary>
        /// The names of the synchronization points of <see cref="MemoryCachingBackend"/>. A test enables a
        /// synchronization point by its name through an <see cref="ITestSynchronizationProvider"/>.
        /// </summary>
        internal static class SyncPoints
        {
            /// <summary>
            /// Reached by a removal or by a step of an invalidation, after the lock of the key has been acquired and
            /// before the item is read.
            /// </summary>
            public const string ItemLocked = "MemoryCachingBackend.RemoveItemImpl:ItemLocked";

            /// <summary>
            /// Reached by an invalidation while it holds the lock of the backward dependency set that it copies.
            /// </summary>
            public const string DependencyLocked = "MemoryCachingBackend.InvalidateDependencyImpl:DependencyLocked";

            /// <summary>
            /// Reached by an invalidation after it has copied a backward dependency set, while it holds no lock.
            /// </summary>
            public const string DependentsCopied = "MemoryCachingBackend.InvalidateDependencyImpl:DependentsCopied";

            /// <summary>
            /// Reached by the registration of a key in a backward dependency set, after the set has been read from
            /// the index and before its lock is acquired.
            /// </summary>
            public const string AddBackwardDependencySetRead = "MemoryCachingBackend.AddBackwardDependency:DependencySetRead";

            /// <summary>
            /// Reached by the removal of a key from a backward dependency set, after the set has been read from the
            /// index and before its lock is acquired.
            /// </summary>
            public const string RemoveBackwardDependencySetRead = "MemoryCachingBackend.RemoveBackwardDependency:DependencySetRead";

            /// <summary>
            /// Reached by a clearing before it removes any item.
            /// </summary>
            public const string ClearRequested = "MemoryCachingBackend.ClearCore:ClearRequested";
        }
    }
}
