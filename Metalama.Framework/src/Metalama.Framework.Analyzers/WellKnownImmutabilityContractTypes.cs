// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Analyzers
{
    /// <summary>
    /// The types that carry the immutability contract without declaring it, matched by metadata name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>IAspect</c> and <c>Fabric</c> declare <c>[ImmutableObject(true)]</c> themselves and do not appear here.
    /// Validators do, because they no longer live in this repository: validation was extracted into the
    /// <c>Metalama.Extensions.Validation</c> package of Metalama.Premium, which this assembly neither references nor
    /// can see. Naming them is the same technique the sibling contract uses to match its own attribute without
    /// referencing <c>Metalama.Framework</c>.
    /// </para>
    /// <para>
    /// <b>These names are unverified.</b> They must be confirmed against Metalama.Premium, and the analyzer reports
    /// <c>LAMA0885</c> at the end of a compilation for any of them that matches no type, so that a stale name is
    /// visible rather than silently inert. The intended end state is that the premium base types carry the attribute
    /// themselves and this table survives only as a fallback for a premium build older than that change.
    /// </para>
    /// </remarks>
    internal static class WellKnownImmutabilityContractTypes
    {
        /// <summary>
        /// Gets the full metadata names of the types that bind their implementations to the contract.
        /// </summary>
        public static readonly ImmutableHashSet<string> Names =
            ImmutableHashSet.Create(
                StringComparer.Ordinal,
                "Metalama.Extensions.Validation.ReferenceValidator",
                "Metalama.Extensions.Validation.BaseReferenceValidator",
                "Metalama.Extensions.Validation.IReferenceValidator" );

        /// <summary>
        /// Determines whether a full metadata name binds its implementations to the contract.
        /// </summary>
        public static bool Contains( string fullMetadataName ) => Names.Contains( fullMetadataName );

        /// <summary>
        /// Gets every name, so that the analyzer can report the ones that match no type.
        /// </summary>
        public static IEnumerable<string> AllNames => Names;
    }
}
