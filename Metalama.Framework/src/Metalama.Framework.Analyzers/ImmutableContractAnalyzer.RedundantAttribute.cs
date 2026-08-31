// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Framework.Analyzers.Immutability
{
    /// <summary>
    /// The rule that reports a marker which states a contract the declaration is bound by anyway.
    /// </summary>
    public partial class ImmutableContractAnalyzer
    {
        /// <remarks>
        /// <para>
        /// The contract propagates to every type that derives from or implements a type that requires it, which is the
        /// whole reason marking <c>IAspect</c> is worth doing. A type that both inherits the obligation and restates it
        /// is checked identically with the marker deleted, so the marker is dead source: it suggests to a reader that
        /// removing it would relax something, when nothing would change.
        /// </para>
        /// <para>
        /// The reason named in the message is not always a marked type. A type of the built-in table of contract
        /// types, and one named by the <c>MetalamaImmutableContractType</c> item, bind their implementations without
        /// carrying the marker, so the message says that the named type requires immutability rather than that it is
        /// marked.
        /// </para>
        /// </remarks>
        internal static readonly DiagnosticDescriptor RedundantImmutableTypeAttribute = new(
            "LAMA0886",
            "An [ImmutableType] attribute is redundant because the contract is already inherited",
            "The [ImmutableType] attribute on '{0}' has no effect, because '{0}' derives from or implements '{1}', "
            + "which already requires it. Remove the attribute.",
            _category,
            DiagnosticSeverity.Warning,
            true,
            customTags: WellKnownDiagnosticTags.Unnecessary );

        private static void AnalyzeRedundantAttribute(
            SymbolAnalysisContext context,
            ImmutabilityContext immutabilityContext,
            INamedTypeSymbol type )
        {
            if ( !ImmutabilityContext.TryGetImmutableTypeAttribute( type, out var attribute ) )
            {
                return;
            }

            var source = immutabilityContext.GetInheritedContractSource( type );

            if ( source == null )
            {
                return;
            }

            var location = SymbolFacts.GetApplicationLocation( attribute );

            if ( location == null )
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    RedundantImmutableTypeAttribute,
                    location,
                    SymbolFacts.GetDisplayName( type ),
                    SymbolFacts.GetDisplayName( source ) ) );
        }
    }
}
