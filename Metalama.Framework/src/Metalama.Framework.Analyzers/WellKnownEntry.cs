// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;

namespace Metalama.Framework.Analyzers.Durability
{
    /// <summary>
    /// An entry of one of the well-known type tables.
    /// </summary>
    internal readonly struct WellKnownEntry
    {
        /// <summary>
        /// Gets the classification of the type.
        /// </summary>
        public WellKnownDurability Durability { get; }

        /// <summary>
        /// Gets the explanation that appears at the end of the retention chain in the diagnostic message, or
        /// <c>null</c> when the classification needs none.
        /// </summary>
        public string? Reason { get; }

        /// <summary>
        /// Gets the indices of the type arguments that must be durable, or the default value when all of them must be.
        /// Relevant only when <see cref="Durability"/> is <see cref="WellKnownDurability.Transparent"/>.
        /// </summary>
        /// <remarks>
        /// The mask exists for <c>ConditionalWeakTable{TKey,TValue}</c>, whose key is not kept alive by the table and
        /// must therefore be ignored.
        /// </remarks>
        public ImmutableArray<int> ArgumentMask { get; }

        public WellKnownEntry( WellKnownDurability durability, string? reason = null, ImmutableArray<int> argumentMask = default )
        {
            this.Durability = durability;
            this.Reason = reason;
            this.ArgumentMask = argumentMask;
        }
    }
}
