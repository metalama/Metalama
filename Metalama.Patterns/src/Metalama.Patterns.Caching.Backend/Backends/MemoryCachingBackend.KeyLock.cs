// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Backends
{
    internal partial class MemoryCachingBackend
    {
        /// <summary>
        /// The lock of a key, with the number of operations that use it.
        /// </summary>
        private sealed class KeyLock
        {
#pragma warning disable SA1401
            /// <summary>
            /// The number of operations that have acquired the lock or are waiting for it.
            /// </summary>
            public int ReferenceCount;

            /// <summary>
            /// Indicates that the lock has been removed from the dictionary and must not be used any more. It is read and
            /// written while the lock is held.
            /// </summary>
            public bool IsRetired;
#pragma warning restore SA1401
        }
    }
}
