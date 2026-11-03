// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Analyzers.Durability
{
    /// <summary>
    /// Determines whether the type of a static member is durable. This is the only API of this assembly that
    /// <c>Metalama.Framework.Engine</c> uses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ImmutabilityValidator</c> in the engine verifies that the static members of compile-time code are durable
    /// (LAMA0892), because only the engine knows the execution scope of a type. It evaluates durability with this class
    /// rather than with a copy of <see cref="DurabilityContext"/>, which stays internal.
    /// </para>
    /// <para>
    /// An instance caches verdicts keyed by symbols, so it must not outlive the compilation of those symbols.
    /// </para>
    /// </remarks>
    public sealed class StaticMemberDurabilityChecker
    {
        private readonly DurabilityContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticMemberDurabilityChecker"/> class.
        /// </summary>
        /// <param name="durableTypes">The type names declared by the <c>MetalamaDurableType</c> item.</param>
        /// <param name="nonDurableTypes">The type names declared by the <c>MetalamaNonDurableType</c> item.</param>
        public StaticMemberDurabilityChecker( IEnumerable<string> durableTypes, IEnumerable<string> nonDurableTypes )
        {
            this._context = DurabilityContext.CreateForStaticMembers( durableTypes, nonDurableTypes );
        }

        /// <summary>
        /// Determines whether a static member is not durable, and if so returns the explanation.
        /// </summary>
        /// <param name="member">The static field or automatic property.</param>
        /// <param name="memberType">The type of <paramref name="member"/>.</param>
        /// <param name="containingTypeName">The display name of the declaring type, which starts the path.</param>
        /// <param name="memberTypeName">The display name of <paramref name="memberType"/>.</param>
        /// <param name="path">The path from the declaring type to the type that is not durable.</param>
        /// <returns><c>true</c> when the member is not durable, or <c>false</c> when it is or is marked <c>[Durable]</c>.</returns>
        public bool IsNotDurable(
            ISymbol member,
            ITypeSymbol memberType,
            string containingTypeName,
            [NotNullWhen( true )] out string? memberTypeName,
            [NotNullWhen( true )] out string? path )
        {
            memberTypeName = null;
            path = null;

            if ( DurabilityContext.HasDurableAttribute( member ) )
            {
                return false;
            }

            var verdict = this._context.GetVerdict( memberType );

            if ( verdict.IsDurable )
            {
                return false;
            }

            memberTypeName = DurabilityContext.GetDisplayName( memberType );
            path = verdict.Prepend( member.Name ).Prepend( containingTypeName ).FormatChain();

            return true;
        }
    }
}
