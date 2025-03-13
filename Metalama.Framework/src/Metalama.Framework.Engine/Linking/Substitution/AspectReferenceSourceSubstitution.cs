// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes an aspect reference that points to the default semantic that points to the original source.
/// </summary>
internal sealed class AspectReferenceSourceSubstitution : AspectReferenceRenamingSubstitution
{
    public AspectReferenceSourceSubstitution( CompilationContext compilationContext, ResolvedAspectReference aspectReference ) : base(
        compilationContext,
        aspectReference )
    {
        // Support only base semantics.
        Invariant.Assert(
            aspectReference.ResolvedSemantic.Kind == IntermediateSymbolSemanticKind.Default
            || aspectReference.ResolvedSemantic is
                { Symbol: { IsOverride: true, IsSealed: false } or { IsVirtual: true }, Kind: IntermediateSymbolSemanticKind.Base } );

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

        return LinkerRewritingDriver.GetOriginalImplMemberName( targetSymbol );
    }

    protected override SyntaxNode SubstituteMemberAccess( MemberAccessExpressionSyntax currentNode, SubstitutionContext substitutionContext )
    {
        var targetSymbol = this.AspectReference.ResolvedSemantic.Symbol;

        var expression =
            targetSymbol.IsStatic
                ? substitutionContext.SyntaxGenerationContext.SyntaxGenerator.TypeSyntax( targetSymbol.ContainingType )
                : this.AspectReference.HasCustomReceiver
                    ? currentNode.Expression
                    : SyntaxFactory.ThisExpression();

        return currentNode.PartialUpdate(
            expression
                .WithTriviaFromIfNecessary( currentNode.Expression, substitutionContext.SyntaxGenerationContext.Options ),
            name: RewriteName( currentNode.Name, this.GetTargetMemberName() ) );
    }
}