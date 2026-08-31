// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace Metalama.Framework.Analyzers.Durability
{
    /// <summary>
    /// The rule that guards the soundness of the waiver that the attribute grants to a member.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On a field or an automatically implemented property, the attribute waives the check on the declared type and
    /// puts the obligation on the values instead. That is the form to use for a member typed as an interface, as
    /// <c>object</c>, or as a delegate, none of which a declared type can vouch for: what a delegate retains is the
    /// closure it was built from, which is visible at the assignment and nowhere else.
    /// </para>
    /// <para>
    /// The exchange is only worth making where the analyzer sees every assignment, and the accessibility of the member
    /// is what decides that. A member that code in another assembly can write may receive a value this compilation
    /// never contains, so the waiver would be granted against a promise nothing checks. Reporting it is the difference
    /// between a contract and a comment.
    /// </para>
    /// </remarks>
    public partial class DurableContractAnalyzer
    {
        internal static readonly DiagnosticDescriptor DurableMemberIsWritableFromOutside = new(
            "LAMA0877",
            "A member marked [Durable] is writable from outside the type that declares it",
            "'{0}' is marked [Durable], so every value assigned to it must be durable, but the member is writable "
            + "from outside '{1}', so not every assignment can be seen. Add the 'readonly' modifier, or make the "
            + "field or the setter private.",
            _category,
            DiagnosticSeverity.Warning,
            true );

        /// <remarks>
        /// Registered on the member rather than reached from the walk over the members of a durable type, because the
        /// attribute is meaningful on a member of a type that is not itself under the contract: the rules on
        /// assignments already check such a member, so this one has to hold there too.
        /// </remarks>
        private static void AnalyzeDurableMember( SymbolAnalysisContext context )
        {
            var member = context.Symbol;

            // The backing field of an automatically implemented property is examined through the property, which is
            // what the author declared and what carries the attribute.
            if ( member is IFieldSymbol { AssociatedSymbol: not null } )
            {
                return;
            }

            if ( !DurabilityContext.HasDurableAttribute( member )
                 || !SymbolFacts.IsWritableFromOutsideDeclaringType( member ) )
            {
                return;
            }

            var location = member.Locations.FirstOrDefault( l => l.IsInSource );

            if ( location == null )
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DurableMemberIsWritableFromOutside,
                    location,
                    member.Name,
                    DurabilityContext.GetDisplayName( member.ContainingType ) ) );
        }
    }
}
