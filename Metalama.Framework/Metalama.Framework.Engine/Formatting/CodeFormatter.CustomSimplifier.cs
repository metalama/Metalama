// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using System;
using System.Linq;

namespace Metalama.Framework.Engine.Formatting;

public sealed partial class CodeFormatter
{
    private sealed class CustomSimplifier : SafeSyntaxRewriter
    {
        private readonly SemanticModel? _semanticModel;

        public CustomSimplifier( SemanticModel? semanticModel )
        {
            // The semantic model is optional, but we cannot do all much useful work if we don't have it.
            this._semanticModel = semanticModel;
        }

        public bool RequiresSemanticModel { get; private set; }

        public override SyntaxNode? DefaultVisit( SyntaxNode node )
        {
            if ( this.RequiresSemanticModel )
            {
                // Return as soon as possible.
                return node;
            }
            else
            {
                return base.DefaultVisit( node );
            }
        }

        public override SyntaxNode VisitObjectCreationExpression( ObjectCreationExpressionSyntax node )
        {
            // We want to simplify delegate creation from `new Action( () => { ... } )` to just `() => { ... }`.
            // This simplification is valid when the delegate is target typed. It is implemented for invocation arguments
            // and assignments.
            if ( node.HasAnnotation( Simplifier.Annotation ) )
            {
                if ( node.ArgumentList?.Arguments is
                         [{ Expression: AnonymousFunctionExpressionSyntax anonymousFunctionExpression }]
                     && node.Parent?.Kind() is SyntaxKind.Argument or SyntaxKind.EqualsValueClause or SyntaxKind.SimpleAssignmentExpression )
                {
                    switch ( node.Parent?.Kind() )
                    {
                        case SyntaxKind.Argument:
                            var argument = (ArgumentSyntax) node.Parent;

                            switch ( node.Parent.Parent?.Parent )
                            {
                                case InvocationExpressionSyntax invocation:
                                    if ( this._semanticModel != null )
                                    {
                                        var symbol = this._semanticModel.GetSymbolInfo( invocation.Expression ).Symbol;
                                        var argumentIndex = invocation.ArgumentList.Arguments.IndexOf( argument );

                                        if ( symbol is IMethodSymbol invokedMethod
                                             && invokedMethod.Parameters[argumentIndex].Type.TypeKind == TypeKind.Delegate )
                                        {
                                            return this.Visit( anonymousFunctionExpression )!;
                                        }
                                    }
                                    else
                                    {
                                        this.RequiresSemanticModel = true;
                                    }

                                    break;

                                case ObjectCreationExpressionSyntax { ArgumentList: not null } objectCreation:
                                    if ( this._semanticModel != null )
                                    {
                                        var symbol = this._semanticModel.GetSymbolInfo( objectCreation ).Symbol;
                                        var argumentIndex = objectCreation.ArgumentList.Arguments.IndexOf( argument );

                                        if ( symbol is IMethodSymbol invokedMethod
                                             && invokedMethod.Parameters[argumentIndex].Type.TypeKind == TypeKind.Delegate )
                                        {
                                            return this.Visit( anonymousFunctionExpression )!;
                                        }
                                    }
                                    else
                                    {
                                        this.RequiresSemanticModel = true;
                                    }

                                    break;

                                default:
                                    if ( node.Parent.IsKind( SyntaxKind.EqualsValueClause ) )
                                    {
                                        return this.Visit( anonymousFunctionExpression )!;
                                    }

                                    break;
                            }

                            break;

                        case SyntaxKind.EqualsValueClause when anonymousFunctionExpression is ParenthesizedLambdaExpressionSyntax
                        {
                            ParameterList.Parameters.Count: 0
                        }:
                            return anonymousFunctionExpression;

                        case SyntaxKind.SimpleAssignmentExpression:
                            if ( this._semanticModel != null )
                            {
                                var assignmentExpression = (AssignmentExpressionSyntax) node.Parent;
                                var symbol = this._semanticModel.GetSymbolInfo( assignmentExpression.Left ).Symbol;

                                if ( symbol is IFieldSymbol { Type.TypeKind: TypeKind.Delegate } or IPropertySymbol { Type.TypeKind: TypeKind.Delegate } )
                                {
                                    return this.Visit( anonymousFunctionExpression )!;
                                }
                            }
                            else
                            {
                                this.RequiresSemanticModel = true;
                            }

                            break;
                    }
                }
            }

            return base.VisitObjectCreationExpression( node )!;
        }

        public override SyntaxNode? VisitCastExpression( CastExpressionSyntax node )
        {
            if ( node.Type.Kind() == SyntaxKind.TupleType && node.HasAnnotation( Simplifier.Annotation ) )
            {
                var tupleType = (TupleTypeSyntax) node.Type;

                if ( tupleType.Elements.All( e => e.Identifier == default ) )
                {
                    if ( this._semanticModel == null )
                    {
                        this.RequiresSemanticModel = true;

                        return node;
                    }
                    else
                    {
                        // Check if this can be simplified. We must compare element by element, ignoring labels, because
                        // Roslyn will return a different type if labels are different.
                        var targetType = this._semanticModel.GetTypeInfo( node.Type ).Type as INamedTypeSymbol;
                        var expressionType = this._semanticModel.GetTypeInfo( node.Expression ).Type as INamedTypeSymbol;

                        if ( targetType != null && expressionType is { Name: nameof(ValueTuple) } && targetType.Arity == expressionType.Arity )
                        {
                            for ( var i = 0; i < targetType.TypeArguments.Length; i++ )
                            {
                                if ( !targetType.TypeArguments[i].Equals( expressionType.TypeArguments[i], SymbolEqualityComparer.IncludeNullability ) )
                                {
                                    // No match.
                                    return node;
                                }
                            }

                            // The types are equal, ignoring labels.
                            return node.Expression;
                        }
                    }
                }
            }

            return base.VisitCastExpression( node );
        }

        public override SyntaxNode? VisitPostfixUnaryExpression( PostfixUnaryExpressionSyntax node )
        {
            if ( node.OperatorToken.IsKind( SyntaxKind.ExclamationToken ) && node.HasAnnotation( Simplifier.Annotation ) )
            {
                if ( this._semanticModel == null )
                {
                    this.RequiresSemanticModel = true;

                    return node;
                }
                else
                {
                    var expressionType = this._semanticModel.GetTypeInfo( node.Operand );

                    if ( expressionType.Type is { NullableAnnotation: NullableAnnotation.NotAnnotated } )
                    {
                        return node.Operand;
                    }
                }
            }

            return base.VisitPostfixUnaryExpression( node );
        }
    }
}