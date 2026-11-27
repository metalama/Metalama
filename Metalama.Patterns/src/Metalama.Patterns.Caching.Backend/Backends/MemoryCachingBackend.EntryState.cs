// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Backends
{
    internal partial class MemoryCachingBackend
    {
        /// <summary>
        /// The state of one entry stored by the current instance. It is read and changed while the lock of the key is held.
        /// </summary>
        private sealed class EntryState
        {
            /// <summary>
            /// Gets or sets a value indicating whether the entry has been removed, replaced or evicted and has been handled.
            /// </summary>
            public bool IsDetached { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the entry is the replacement value (tombstone) of a removed item.
            /// </summary>
            public bool IsReplacement { get; init; }

            /// <summary>
            /// Gets or sets a value indicating whether the entry is a copy of an item of another layer that was read
            /// without its dependencies. The dependencies of such an entry are not registered in the dependency index.
            /// </summary>
            public bool HasUnknownDependencies { get; init; }

            /// <summary>
            /// Gets the time, in UTC ticks, at which the computation of the value of the entry started, or zero when it is
            /// not known. An entry whose computation started later is not replaced by a conditional store. See
            /// <see cref="CachingBackend.SetItem"/>.
            /// </summary>
            public long ComputationStartTicks { get; init; }
        }
    }
}
