// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;

namespace Metalama.Framework.Analyzers
{
    /// <summary>
    /// An entry of the well-known immutable type table.
    /// </summary>
    internal readonly struct WellKnownImmutabilityEntry
    {
        /// <summary>
        /// Gets the classification of the type.
        /// </summary>
        public WellKnownImmutability Immutability { get; }

        /// <summary>
        /// Gets the explanation that appears at the end of the chain in the diagnostic message, or <c>null</c> when
        /// the classification needs none.
        /// </summary>
        public string? Reason { get; }

        /// <summary>
        /// Gets the indices of the type arguments that must be immutable, or the default value when all of them must
        /// be. Relevant only when <see cref="Immutability"/> is <see cref="WellKnownImmutability.Transparent"/>.
        /// </summary>
        public ImmutableArray<int> ArgumentMask { get; }

        public WellKnownImmutabilityEntry(
            WellKnownImmutability immutability,
            string? reason = null,
            ImmutableArray<int> argumentMask = default )
        {
            this.Immutability = immutability;
            this.Reason = reason;
            this.ArgumentMask = argumentMask;
        }
    }
}
