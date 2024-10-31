// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

internal abstract partial class AspectReferenceRenamingSubstitution : SyntaxNodeSubstitution
{
    protected ResolvedAspectReference AspectReference { get; }

    public override SyntaxNode ReplacedNode => this.AspectReference.RootNode;

    protected AspectReferenceRenamingSubstitution( CompilationContext compilationContext, ResolvedAspectReference aspectReference ) : base( compilationContext )
    {
        this.AspectReference = aspectReference;
    }

    public sealed override SyntaxNode? Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        if ( this.AspectReference.RootNode != this.AspectReference.SymbolSourceNode )
        {
            // Root node is different that symbol source node - this is introduction in form:
            // <helper_type>.<helper_member>(<symbol_source_node>);
            // We need to get to symbol source node.

            currentNode = this.AspectReference.RootNode switch
            {
                InvocationExpressionSyntax { ArgumentList: { Arguments.Count: 1 } argumentList } =>
                    argumentList.Arguments[0].Expression,
                _ => throw new AssertionFailedException( $"Unsupported form: {this.AspectReference.RootNode}" )
            };
        }

        switch ( currentNode )
        {
            case MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.Text: LinkerInjectionHelperProvider.HelperTypeName },
                Name.Identifier.Text: LinkerInjectionHelperProvider.FinalizeMemberName
            } finalizerMemberAccess:
                return this.SubstituteFinalizerMemberAccess( finalizerMemberAccess );

            case MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.Text: LinkerInjectionHelperProvider.HelperTypeName },
                Name.Identifier.Text: var operatorName
            } when SymbolHelpers.GetOperatorKindFromName( operatorName ) != OperatorKind.None:
                return this.SubstituteOperatorMemberAccess( substitutionContext );

            case ObjectCreationExpressionSyntax:
                return this.SubstituteConstructorMemberAccess();

            case MemberAccessExpressionSyntax memberAccessExpression:
                return this.SubstituteMemberAccess( memberAccessExpression, substitutionContext );

            case ElementAccessExpressionSyntax elementAccessExpression:
                return this.SubstituteElementAccess( elementAccessExpression );

            case ConditionalAccessExpressionSyntax conditionalAccessExpression:
                return this.SubstituteConditionalAccess( conditionalAccessExpression );

            default:
                throw new AssertionFailedException( $"Unsupported: {currentNode}" );
        }
    }

    protected abstract string GetTargetMemberName();

    protected virtual SyntaxNode SubstituteFinalizerMemberAccess( MemberAccessExpressionSyntax currentNode )
        => MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            ThisExpression(),
            IdentifierName( this.GetTargetMemberName() ) );

    private SyntaxNode SubstituteConstructorMemberAccess()
        => MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            ThisExpression(),
            IdentifierName( this.GetTargetMemberName() ) );

    private SyntaxNode SubstituteOperatorMemberAccess( SubstitutionContext substitutionContext )
    {
        var targetSymbol = this.AspectReference.ResolvedSemantic.Symbol;

        return
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                substitutionContext.SyntaxGenerationContext.SyntaxGenerator.TypeSyntax( targetSymbol.ContainingType ),
                IdentifierName( this.GetTargetMemberName() ) );
    }

    protected abstract SyntaxNode? SubstituteMemberAccess( MemberAccessExpressionSyntax currentNode, SubstitutionContext substitutionContext );

    protected virtual SyntaxNode SubstituteElementAccess( ElementAccessExpressionSyntax currentNode )
        => throw new NotSupportedException( $"Element access is not supported by {this.GetType().Name}" );

    private SyntaxNode SubstituteConditionalAccess( ConditionalAccessExpressionSyntax currentNode )
    {
        var targetSymbol = this.AspectReference.ResolvedSemantic.Symbol;

        if ( this.CompilationContext.SymbolComparer.IsConvertibleTo(
                this.AspectReference.ContainingSemantic.Symbol.ContainingType,
                targetSymbol.ContainingType ) )
        {
            if ( this.AspectReference.OriginalSymbol.IsInterfaceMemberImplementation() )
            {
                throw new AssertionFailedException( Justifications.CoverageMissing );
            }
            else
            {
                var rewriter = new ConditionalAccessRewriter( this.GetTargetMemberName() );

                return (ExpressionSyntax) rewriter.Visit( currentNode )!;
            }
        }
        else if ( this.CompilationContext.SymbolComparer.IsConvertibleTo(
                     targetSymbol.ContainingType,
                     this.AspectReference.ContainingSemantic.Symbol.ContainingType ) )
        {
            throw new AssertionFailedException( $"Resolved symbol '{this.AspectReference.ContainingSemantic.Symbol}' is declared in a derived class." );
        }
        else
        {
            var rewriter = new ConditionalAccessRewriter( this.GetTargetMemberName() );

            return (ExpressionSyntax) rewriter.Visit( currentNode )!;
        }
    }

    protected static SimpleNameSyntax RewriteName( SimpleNameSyntax name, string targetMemberName )
        => name switch
        {
            GenericNameSyntax genericName => genericName.WithIdentifier( Identifier( targetMemberName.AssertNotNull() ) ),
            IdentifierNameSyntax _ => name.WithIdentifier( Identifier( targetMemberName.AssertNotNull() ) ),
            _ => throw new AssertionFailedException( $"Unsupported name: {name}" )
        };
}