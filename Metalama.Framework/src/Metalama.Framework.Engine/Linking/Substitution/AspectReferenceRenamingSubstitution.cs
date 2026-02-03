// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

internal abstract partial class AspectReferenceRenamingSubstitution : SyntaxNodeSubstitution
{
    protected ResolvedAspectReference AspectReference { get; }

    public override SyntaxNode ReplacedNode { get; }

    protected AspectReferenceRenamingSubstitution( CompilationContext compilationContext, ResolvedAspectReference aspectReference ) : base( compilationContext )
    {
        this.AspectReference = aspectReference;

        if ( aspectReference.RootNode.IsKind( SyntaxKind.SimpleMemberAccessExpression )
             && aspectReference.RootNode is
                 MemberAccessExpressionSyntax
                 {
                     Expression: IdentifierNameSyntax { Identifier.Text: LinkerInjectionHelperProvider.HelperTypeName },
                     Name.Identifier.Text: var methodName
                 }
             && OperatorData.GetByName( methodName ) is { IsStatic: false } )
        {
            // For operators with static receiver parameter, we replace the parent node.
            this.ReplacedNode = aspectReference.RootNode.Parent.AssertNotNull();
        }
        else
        {
            this.ReplacedNode = aspectReference.RootNode;
        }
    }

    public sealed override SyntaxNode? Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        if ( this.AspectReference.RootNode != this.AspectReference.SymbolSourceNode )
        {
            // Root node is different that symbol source node - this is introduction in form:
            // <helper_type>.<helper_member>(<symbol_source_node>);
            // We need to get to symbol source node.

            currentNode = this.AspectReference.RootNode.Kind() switch
            {
                SyntaxKind.InvocationExpression when this.AspectReference.RootNode is InvocationExpressionSyntax { ArgumentList: { Arguments.Count: 1 } argumentList } =>
                    argumentList.Arguments[0].Expression,
                _ => throw new AssertionFailedException( $"Unsupported form: {this.AspectReference.RootNode}" )
            };
        }

        switch ( currentNode.Kind() )
        {
            case SyntaxKind.SimpleMemberAccessExpression when currentNode is MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.Text: LinkerInjectionHelperProvider.HelperTypeName },
                Name.Identifier.Text: LinkerInjectionHelperProvider.FinalizeMemberName
            } finalizerMemberAccess:
                return this.SubstituteFinalizerMemberAccess( finalizerMemberAccess );

            case SyntaxKind.SimpleMemberAccessExpression when currentNode is MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.Text: LinkerInjectionHelperProvider.HelperTypeName },
                Name.Identifier.Text: var operatorName
            } && OperatorData.GetByName( operatorName ) is { IsStatic: var isStaticOperator }:
                // We presume static operators - non-static operators need to change the argument list, not just the name.
                Invariant.Assert( isStaticOperator );

                return this.SubstituteOperatorMemberAccess( substitutionContext );

            case SyntaxKind.InvocationExpression when currentNode is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.Text: LinkerInjectionHelperProvider.HelperTypeName },
                    Name.Identifier.Text: var operatorName
                }
            } invocationExpression && OperatorData.GetByName( operatorName ) is { IsStatic: var isStaticOperator }:
                // We presume static operators - non-static operators need to change the argument list, not just the name.
                Invariant.Assert( !isStaticOperator );

                return this.SubstituteStaticReceiverOperatorInvocation( invocationExpression );

            case SyntaxKind.ObjectCreationExpression:
                return this.SubstituteConstructorMemberAccess();

            case SyntaxKind.SimpleMemberAccessExpression when currentNode is MemberAccessExpressionSyntax memberAccessExpression:
                return this.SubstituteMemberAccess( memberAccessExpression, substitutionContext );

            case SyntaxKind.ElementAccessExpression when currentNode is ElementAccessExpressionSyntax elementAccessExpression:
                return this.SubstituteElementAccess( elementAccessExpression );

            case SyntaxKind.ConditionalAccessExpression when currentNode is ConditionalAccessExpressionSyntax conditionalAccessExpression:
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
            SyntaxFactoryEx.WellKnownIdentifierName( this.GetTargetMemberName() ) );

    private SyntaxNode SubstituteConstructorMemberAccess()
        => MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            ThisExpression(),
            SyntaxFactoryEx.WellKnownIdentifierName( this.GetTargetMemberName() ) );

    private SyntaxNode SubstituteOperatorMemberAccess( SubstitutionContext substitutionContext )
    {
        var targetSymbol = this.AspectReference.ResolvedSemantic.Symbol;

        var memberAccess =
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                substitutionContext.SyntaxGenerationContext.SyntaxGenerator.TypeSyntax( targetSymbol.ContainingType ),
                SyntaxFactoryEx.WellKnownIdentifierName( this.GetTargetMemberName() ) );

        return memberAccess;
    }

    private SyntaxNode SubstituteStaticReceiverOperatorInvocation( InvocationExpressionSyntax invocationExpression )
    {
        var memberAccess =
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    invocationExpression.ArgumentList.Arguments[0].Expression,
                    SyntaxFactoryEx.WellKnownIdentifierName( this.GetTargetMemberName() ) ),
                ArgumentList( SeparatedList( invocationExpression.ArgumentList.Arguments.Skip( 1 ) ) ) );

        return memberAccess;
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
            if ( this.AspectReference.OriginalSymbol.IsExplicitInterfaceMemberImplementation() )
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
        => name.Kind() switch
        {
            SyntaxKind.GenericName when name is GenericNameSyntax genericName => genericName.WithIdentifier( SyntaxFactoryEx.WellKnownIdentifier( targetMemberName.AssertNotNull() ) ),
            SyntaxKind.IdentifierName => name.WithIdentifier( SyntaxFactoryEx.WellKnownIdentifier( targetMemberName.AssertNotNull() ) ),
            _ => throw new AssertionFailedException( $"Unsupported name: {name}" )
        };
}