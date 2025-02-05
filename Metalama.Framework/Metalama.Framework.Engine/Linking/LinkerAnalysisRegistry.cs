// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Linking.Substitution;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Contains information collected during analysis of the intermediate assembly and provides methods querying this information.
/// </summary>
internal sealed class LinkerAnalysisRegistry
{
    private readonly HashSet<IntermediateSymbolSemantic> _reachableSemantics;
    private readonly HashSet<IntermediateSymbolSemantic> _inlinedSemantics;
    private readonly IReadOnlyDictionary<InliningContextIdentifier, IReadOnlyDictionary<SyntaxNode, SyntaxNodeSubstitution>> _substitutions;
    private readonly HashSet<ISymbol> _overrideTargetsWithUnsupportedNonInlinedOverrides;

    public LinkerAnalysisRegistry(
        CompilationContext intermediateCompilation,
        HashSet<IntermediateSymbolSemantic> reachableSemantics,
        HashSet<IntermediateSymbolSemantic> inlinedSemantics,
        IReadOnlyDictionary<InliningContextIdentifier, IReadOnlyList<SyntaxNodeSubstitution>> substitutions,
        HashSet<ISymbol> overrideTargetsWithUnsupportedNonInlinedOverrides )
    {
        this._reachableSemantics = reachableSemantics;
        this._inlinedSemantics = inlinedSemantics;
        this._overrideTargetsWithUnsupportedNonInlinedOverrides = overrideTargetsWithUnsupportedNonInlinedOverrides;

        this._substitutions =
            substitutions.ToDictionary(
                x => x.Key,
                x => (IReadOnlyDictionary<SyntaxNode, SyntaxNodeSubstitution>) x.Value.ToDictionary( y => y.ReplacedNode, y => y ),
                InliningContextIdentifierEqualityComparer.ForCompilation( intermediateCompilation ) );
    }

    public bool IsReachable( IntermediateSymbolSemantic semantic ) => this._reachableSemantics.Contains( semantic );

    public bool IsInlined( IntermediateSymbolSemantic semantic ) => this._inlinedSemantics.Contains( semantic );

    public IReadOnlyDictionary<SyntaxNode, SyntaxNodeSubstitution>? GetSubstitutions( InliningContextIdentifier contextId )
    {
        if ( !this._substitutions.TryGetValue( contextId, out var substitutions ) )
        {
            return null;
        }

        return substitutions;
    }

    public bool HasAnySubstitutions( ISymbol symbol )
    {
        switch ( symbol )
        {
            case IMethodSymbol methodSymbol:
                var semantic = methodSymbol.ToSemantic( IntermediateSymbolSemanticKind.Default );
                var rootContextId = new InliningContextIdentifier( semantic );

                if ( this._substitutions.TryGetValue( rootContextId, out var substitutions ) )
                {
                    return substitutions.Values.Any();
                }
                else
                {
                    return false;
                }

            case IEventSymbol eventSymbol:
                return
                    (eventSymbol.AddMethod != null && this.HasAnySubstitutions( eventSymbol.AddMethod ))
                    || (eventSymbol.RemoveMethod != null && this.HasAnySubstitutions( eventSymbol.RemoveMethod ));

            case IPropertySymbol propertySymbol:
                return
                    (propertySymbol.GetMethod != null && this.HasAnySubstitutions( propertySymbol.GetMethod ))
                    || (propertySymbol.SetMethod != null && this.HasAnySubstitutions( propertySymbol.SetMethod ));

            default:
                return false;
        }
    }

    public bool HasBaseSemanticReferences( ISymbol symbol ) => this.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) );

    public bool HasAnyUnsupportedOverride( ISymbol symbol ) => this._overrideTargetsWithUnsupportedNonInlinedOverrides.Contains( symbol );
}