// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Framework.Analyzers.Durability
{
    /// <summary>
    /// The rule that reports an attribute which states a contract the declaration is bound by anyway.
    /// </summary>
    public partial class DurableContractAnalyzer
    {
        /// <remarks>
        /// <para>
        /// The contract propagates to every type that derives from or implements a type carrying the attribute, which
        /// is the whole reason marking an interface is worth doing. A type that both inherits the obligation and
        /// restates it is checked identically with the attribute deleted, so the attribute is dead source: it suggests
        /// to a reader that removing it would relax something, when nothing would change.
        /// </para>
        /// <para>
        /// The diagnostic is reported on the attribute rather than on the type, and carries the <c>Unnecessary</c> tag
        /// so that the editor fades exactly the text to delete.
        /// </para>
        /// </remarks>
        internal static readonly DiagnosticDescriptor RedundantDurableAttribute = new(
            "LAMA0874",
            "A [Durable] attribute is redundant because the contract is already inherited",
            "The [Durable] attribute on '{0}' has no effect, because '{0}' derives from or implements '{1}', which "
            + "already requires it. Remove the attribute.",
            _category,
            DiagnosticSeverity.Warning,
            true,
            customTags: WellKnownDiagnosticTags.Unnecessary );

        private static void AnalyzeRedundantAttribute( SymbolAnalysisContext context, INamedTypeSymbol type )
        {
            if ( !DurabilityContext.TryGetDurableAttribute( type, out var attribute ) )
            {
                return;
            }

            var source = DurabilityContext.GetInheritedContractSource( type );

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
                    RedundantDurableAttribute,
                    location,
                    DurabilityContext.GetDisplayName( type ),
                    DurabilityContext.GetDisplayName( source ) ) );
        }
    }
}
