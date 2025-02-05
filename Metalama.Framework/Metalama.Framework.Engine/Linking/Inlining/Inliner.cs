// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Inlining;

/// <summary>
/// Allows for one kind of inlining of aspect references.
/// </summary>
internal abstract class Inliner
{
    /// <summary>
    /// Determines whether the inliner can be used for the specified target symbol.
    /// </summary>
    /// <param name="symbol">Target symbol.</param>
    /// <returns></returns>
    public abstract bool IsValidForTargetSymbol( ISymbol symbol );

    // ReSharper disable once UnusedParameter.Global

    /// <summary>
    /// Determines whether the inliner can be used for the specified containing symbol.
    /// </summary>
    /// <param name="symbol">Containing symbol.</param>
    /// <returns></returns>
    public abstract bool IsValidForContainingSymbol( ISymbol symbol );

    /// <summary>
    /// Determines whether an aspect reference can be inlined.
    /// </summary>
    /// <param name="aspectReference">Resolved aspect reference.</param>
    /// <param name="semanticModel">Semantic model of the syntax tree that contains the reference.</param>
    /// <returns></returns>
    public virtual bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel )
    {
        if ( !SymbolEqualityComparer.Default.Equals(
                aspectReference.ContainingSemantic.Symbol.ContainingType,
                aspectReference.ResolvedSemantic.Symbol.ContainingType ) )
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the inlining info during analysis.
    /// </summary>
    /// <param name="aspectReference">Aspect reference to inline.</param>
    /// <returns>Inlining specification.</returns>
    public abstract InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference );

    /// <summary>
    /// Inlines the target of the annotated expression by specifying node to be replaced and the replacing node.
    /// </summary>
    /// <param name="syntaxGenerationContext"></param>
    /// <param name="specification">Inlining specification.</param>
    /// <param name="currentNode">Current node (after substitutions).</param>
    /// <param name="linkedTargetBody">Linked target body that is to be inlined.</param>
    /// <returns>Statement resulting from inlining.</returns>
    public virtual StatementSyntax Inline(
        SyntaxGenerationContext syntaxGenerationContext,
        InliningSpecification specification,
        SyntaxNode currentNode,
        StatementSyntax linkedTargetBody )
        => linkedTargetBody.AddTriviaFromIfNecessary( currentNode, syntaxGenerationContext.Options );
}