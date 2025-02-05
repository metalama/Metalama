// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Tests.LinkerTests.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Tests.LinkerTests.Runner
{
    internal partial class LinkerTestInputBuilder
    {
        private sealed class TestMethodBodyRewriter : SafeSyntaxRewriter
        {
            private readonly string _aspectName;
            private readonly string? _layerName;

            public TestMethodBodyRewriter( string aspectName, string? layerName )
            {
                this._aspectName = aspectName;
                this._layerName = layerName;
            }

            public override SyntaxNode? VisitInvocationExpression( InvocationExpressionSyntax node )
            {
                if ( node.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName } memberAccess
                     && StringComparer.Ordinal.Equals( genericName.Identifier.ValueText, nameof(Api._cast) ) )
                {
                    return ParenthesizedExpression(
                        CastExpression(
                            genericName.TypeArgumentList.Arguments.Single(),
                            (ExpressionSyntax) this.Visit( memberAccess.Expression ).AssertNotNull() ) );
                }

                if ( this.TransformInvocationOrElementAccess( node, node.Expression, node.ArgumentList.Arguments, out var transformedNode ) )
                {
                    return transformedNode;
                }

                return base.VisitInvocationExpression( node );
            }

            public override SyntaxNode? VisitElementAccessExpression( ElementAccessExpressionSyntax node )
            {
                if ( this.TransformInvocationOrElementAccess( node, node.Expression, node.ArgumentList.Arguments, out var transformedNode ) )
                {
                    return transformedNode;
                }

                return base.VisitElementAccessExpression( node );
            }

            private bool TransformInvocationOrElementAccess(
                SyntaxNode originalNode,
                ExpressionSyntax expression,
                SeparatedSyntaxList<ArgumentSyntax> arguments,
                [NotNullWhen( true )] out SyntaxNode? transformedNode )
            {
                if ( expression is IdentifierNameSyntax { Identifier.ValueText: "link" } )
                {
                    // TODO: Annotation order.
                    if ( arguments.Count is < 1 or > 3 )
                    {
                        throw new ArgumentException( "link method should have 1 to 3 arguments." );
                    }

                    var annotatedExpression = arguments[0].Expression;

                    AspectReferenceFlags flags = default;

                    // Since most of the linker tests linking normal aspects, the default order for linker tests is Previous.
                    var order = AspectReferenceOrder.Previous;

                    for ( var i = 1; i < arguments.Count; i++ )
                    {
                        var tag = arguments[i].ToString();

                        switch ( tag )
                        {
                            case "inline":
                                flags |= AspectReferenceFlags.Inlineable;

                                break;

                            case "@base":
                                order = AspectReferenceOrder.Base;

                                break;

                            case "previous":
                                order = AspectReferenceOrder.Previous;

                                break;

                            case "current":
                                order = AspectReferenceOrder.Current;

                                break;

                            case "final":
                                order = AspectReferenceOrder.Final;

                                break;

                            default:
                                throw new ArgumentException( $"unsupported link[] tag {tag}" );
                        }
                    }

                    var target = AspectReferenceTargetKind.Self;

                    if ( annotatedExpression is MemberAccessExpressionSyntax memberAccess )
                    {
                        switch ( memberAccess.Name.Identifier.ValueText )
                        {
                            case "get":
                                annotatedExpression = memberAccess.Expression;
                                target = AspectReferenceTargetKind.PropertyGetAccessor;

                                break;

                            case "set":
                                annotatedExpression = memberAccess.Expression;
                                target = AspectReferenceTargetKind.PropertySetAccessor;

                                break;

                            case "add":
                                annotatedExpression = memberAccess.Expression;
                                target = AspectReferenceTargetKind.EventAddAccessor;

                                break;

                            case "remove":
                                annotatedExpression = memberAccess.Expression;
                                target = AspectReferenceTargetKind.EventRemoveAccessor;

                                break;

                            case "raise":
                                annotatedExpression = memberAccess.Expression;
                                target = AspectReferenceTargetKind.EventRaiseAccessor;

                                break;
                        }
                    }

                    transformedNode =
                        this.Visit( annotatedExpression )!
                            .WithAspectReferenceAnnotation( new AspectLayerId( this._aspectName, this._layerName ), order, target, flags )
                            .WithLeadingTrivia( originalNode.GetLeadingTrivia() )
                            .WithTrailingTrivia( originalNode.GetTrailingTrivia() );

                    return true;
                }

                transformedNode = null;

                return false;
            }

            public override SyntaxNode? VisitMemberAccessExpression( MemberAccessExpressionSyntax node )
            {
                if ( node.Kind() == SyntaxKind.SimpleMemberAccessExpression
                     && node.Expression is IdentifierNameSyntax identifier
                     && (StringComparer.Ordinal.Equals( identifier.Identifier.ValueText, nameof(Api._static) )
                         || StringComparer.Ordinal.Equals( identifier.Identifier.ValueText, nameof(Api._local) )) )
                {
                    return node.Name;
                }

                return base.VisitMemberAccessExpression( node );
            }

            public override SyntaxNode? VisitIdentifierName( IdentifierNameSyntax node )
            {
                if ( StringComparer.Ordinal.Equals( node.Identifier.ValueText, nameof(Api._this) ) )
                {
                    return ThisExpression();
                }

                return base.VisitIdentifierName( node );
            }
        }
    }
}