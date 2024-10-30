// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes an aspect reference that points to the base declaration (override or a hidden slot).
/// </summary>
internal sealed class AspectReferenceEmptySubstitution : AspectReferenceRenamingSubstitution
{
    public AspectReferenceEmptySubstitution( CompilationContext compilationContext, ResolvedAspectReference aspectReference ) : base(
        compilationContext,
        aspectReference )
    {
        // Support only base semantics.
        Invariant.Assert( aspectReference.ResolvedSemantic.Kind == IntermediateSymbolSemanticKind.Base );

        Invariant.Assert(
            !aspectReference.ResolvedSemantic.Symbol.IsOverride
            && !aspectReference.ResolvedSemantic.Symbol.TryGetHiddenSymbol( this.CompilationContext.Compilation, out _ ) );

        // Auto properties and event field default semantics should not get here.
        Invariant.AssertNot(
            aspectReference.ResolvedSemantic is { Kind: IntermediateSymbolSemanticKind.Default, Symbol: IPropertySymbol property }
            && property.IsAutoProperty() == true );

        Invariant.AssertNot(
            aspectReference.ResolvedSemantic is { Kind: IntermediateSymbolSemanticKind.Default, Symbol: IEventSymbol @event }
            && @event.IsEventField() == true );
    }

    protected override string GetTargetMemberName()
    {
        var targetSymbol = this.AspectReference.ResolvedSemantic.Symbol;

        return LinkerRewritingDriver.GetEmptyImplMemberName( targetSymbol );
    }

    protected override SyntaxNode SubstituteMemberAccess( MemberAccessExpressionSyntax currentNode, SubstitutionContext substitutionContext )
    {
        var targetSymbol = this.AspectReference.ResolvedSemantic.Symbol;

        ExpressionSyntax expression =
            targetSymbol.IsStatic
                ? substitutionContext.SyntaxGenerationContext.SyntaxGenerator.TypeSyntax( targetSymbol.ContainingType )
                : ThisExpression();

        return currentNode.PartialUpdate(
            expression: expression
                .WithTriviaFromIfNecessary( currentNode.Expression, substitutionContext.SyntaxGenerationContext.Options ),
            name: RewriteName( currentNode.Name, this.GetTargetMemberName() ) );
    }
}