// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes an aspect reference that points to the base declaration (override or hidden slot).
/// </summary>
internal sealed class AspectReferenceBaseSubstitution : AspectReferenceRenamingSubstitution
{
    private readonly SyntaxGenerationOptions _syntaxGenerationOptions;

    public AspectReferenceBaseSubstitution(
        CompilationContext compilationContext,
        ResolvedAspectReference aspectReference,
        SyntaxGenerationOptions syntaxGenerationOptions ) : base(
        compilationContext,
        aspectReference )
    {
        this._syntaxGenerationOptions = syntaxGenerationOptions;

        // Support only base semantics.
        Invariant.Assert( aspectReference.ResolvedSemantic.Kind == IntermediateSymbolSemanticKind.Base );

        Invariant.Assert(
            aspectReference.ResolvedSemantic.Symbol.IsOverride
            || aspectReference.ResolvedSemantic.Symbol.TryGetHiddenSymbol( this.CompilationContext.Compilation, out _ )
            || !compilationContext.SymbolComparer.Equals(
                aspectReference.ContainingSemantic.Symbol.ContainingType,
                aspectReference.ResolvedSemantic.Symbol.ContainingType ) );

        // Auto properties and event field default semantics should not get here.
        Invariant.AssertNot(
            aspectReference.ResolvedSemantic is { Kind: IntermediateSymbolSemanticKind.Default, Symbol: IPropertySymbol property }
            && property.IsAutoProperty() == true );

        Invariant.AssertNot(
            aspectReference.ResolvedSemantic is { Kind: IntermediateSymbolSemanticKind.Default, Symbol: IEventSymbol @event }
            && @event.IsEventField() == true );

        if ( aspectReference is { HasCustomReceiver: true, ResolvedSemantic.Symbol.IsStatic: false } )
        {
            throw AspectLinkerDiagnosticDescriptors.CannotInvokeAnotherInstanceBaseRequired.CreateException( aspectReference.OriginalSymbol );
        }
    }

    protected override string GetTargetMemberName()
    {
        var targetSymbol = this.AspectReference.ResolvedSemantic.Symbol;

        return targetSymbol.Name;
    }

    protected override SyntaxNode SubstituteFinalizerMemberAccess( MemberAccessExpressionSyntax currentNode )
        => IdentifierName( "__LINKER_TO_BE_REMOVED__" )
            .WithLinkerGeneratedFlags( LinkerGeneratedFlags.NullAspectReferenceExpression )
            .WithTriviaFromIfNecessary( currentNode, this._syntaxGenerationOptions );

    protected override SyntaxNode SubstituteMemberAccess( MemberAccessExpressionSyntax currentNode, SubstitutionContext substitutionContext )
    {
        var targetSymbol = this.AspectReference.ResolvedSemantic.Symbol;

        if ( targetSymbol.IsStatic )
        {
            if ( !targetSymbol.TryGetHiddenSymbol( this.CompilationContext.Compilation, out var hiddenSymbol ) )
            {
                throw new AssertionFailedException( $"Expected hidden symbol for '{targetSymbol}'." );
            }

            return currentNode
                .WithExpression(
                    substitutionContext.SyntaxGenerationContext.SyntaxGenerator.TypeSyntax( hiddenSymbol.ContainingType )
                        .WithTriviaFromIfNecessary( currentNode.Expression, this._syntaxGenerationOptions ) );
        }
        else
        {
            return currentNode
                .WithExpression(
                    BaseExpression()
                        .WithTriviaFromIfNecessary( currentNode.Expression, this._syntaxGenerationOptions ) );
        }
    }

    protected override SyntaxNode SubstituteElementAccess( ElementAccessExpressionSyntax currentNode )
        => currentNode
            .WithExpression(
                BaseExpression()
                    .WithTriviaFromIfNecessary( currentNode.Expression, this._syntaxGenerationOptions ) );
}