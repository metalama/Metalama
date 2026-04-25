// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Templating
{
    internal sealed partial class TemplateCompilerRewriter
    {
        private sealed class CompileTimeOnlyRewriter : SafeSyntaxRewriter
        {
            private readonly TemplateCompilerRewriter _parent;
            private readonly ContextualSyntaxGenerator _syntaxGenerator;

            public CompileTimeOnlyRewriter( TemplateCompilerRewriter parent )
            {
                this._parent = parent;
                this._syntaxGenerator = this._parent.MetaSyntaxFactory.SyntaxGenerationContext.SyntaxGenerator;
            }

            public override SyntaxNode VisitTypeOfExpression( TypeOfExpressionSyntax node )
            {
                var symbol = this._parent._syntaxTreeAnnotationMap.GetSymbol( node.Type );

                if ( symbol?.Kind is SymbolKind.NamedType or SymbolKind.ArrayType or SymbolKind.PointerType or SymbolKind.FunctionPointerType
                         or SymbolKind.DynamicType or SymbolKind.ErrorType or SymbolKind.TypeParameter
                     && symbol is ITypeSymbol typeSymbol )
                {
                    var typeOfString = this._syntaxGenerator.TypeOfExpression( typeSymbol ).ToString();

                    return this._parent._typeOfRewriter.RewriteTypeOf(
                            typeSymbol,
                            this._parent.CreateTypeParameterSubstitutionDictionary(
                                nameof(TemplateTypeArgument.Type),
                                this._parent._dictionaryOfITypeType ) )
                        .WithAdditionalAnnotations( new SyntaxAnnotation( _rewrittenTypeOfAnnotation, typeOfString ) );
                }
                else
                {
                    return node;
                }
            }

            public bool TryRewriteProceedInvocation( InvocationExpressionSyntax node, out InvocationExpressionSyntax transformedNode )
            {
                var kind = this._parent._templateMemberClassifier.GetMetaMemberKind( node.Expression );

                if ( kind.IsAnyProceed() )
                {
                    var methodName = node.Expression.Kind() switch
                    {
                        SyntaxKind.SimpleMemberAccessExpression when node.Expression is MemberAccessExpressionSyntax memberAccess => memberAccess.Name
                            .Identifier.Text,
                        SyntaxKind.IdentifierName when node.Expression is IdentifierNameSyntax identifier => identifier.Identifier.Text,
                        _ => throw new AssertionFailedException( $"Don't know how to get the member name in {node.Expression.GetType().Name}" )
                    };

                    // ReSharper disable once RedundantSuppressNullableWarningExpression
                    transformedNode =
                        node.CopyAnnotationsTo(
                            InvocationExpression(
                                    this._parent._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.Proceed) ) )
                                .WithArgumentList( ArgumentList( SeparatedList( [Argument( SyntaxFactoryEx.LiteralExpression( methodName ) )] ) ) ) )!;

                    return true;
                }

                // meta.ProceedAsync().ConfigureAwait(false) is also treated like a Proceed() expression
                else if ( this._parent._syntaxTreeAnnotationMap.GetSymbol( node ).IsTaskConfigureAwait()
                          && node is

                              // ReSharper disable once MissingIndent
                              {
                                  Expression: MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax innerInvocation },
                                  ArgumentList.Arguments: [{ Expression: var expression }]
                              }
                          && this.TryRewriteProceedInvocation( innerInvocation, out var transformedInner ) )
                {
                    if ( expression.Kind() is not (SyntaxKind.CharacterLiteralExpression or SyntaxKind.StringLiteralExpression
                             or SyntaxKind.NumericLiteralExpression or SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression
                             or SyntaxKind.NullLiteralExpression or SyntaxKind.DefaultLiteralExpression)
                         || expression is not LiteralExpressionSyntax literal )
                    {
                        this._parent.Report(
                            TemplatingDiagnosticDescriptors.OnlyLiteralArgumentInConfigureAwaitAfterProceedAsync.CreateRoslynDiagnostic(
                                expression.GetLocation(),
                                expression.ToString() ) );

                        transformedNode = node;

                        return false;
                    }

                    // *.ConfigureAwait( transformedInner, true/false )
                    transformedNode =
                        node.CopyAnnotationsTo(
                            InvocationExpression(
                                    this._parent._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.ConfigureAwait) ) )
                                .AddArgumentListArguments(
                                    Argument( transformedInner ),
                                    Argument( literal ) ) );

                    return true;
                }
                else
                {
                    transformedNode = node;

                    return false;
                }
            }

            public override SyntaxNode? VisitInvocationExpression( InvocationExpressionSyntax node )
            {
                if ( this.TryRewriteProceedInvocation( node, out var transformedNode ) )
                {
                    return transformedNode;
                }
                else if ( node.IsNameOf() )
                {
                    var type = this._parent._syntaxTreeAnnotationMap.GetSymbol( node.ArgumentList.Arguments[0].Expression );

                    if ( type != null )
                    {
                        return SyntaxFactoryEx.LiteralExpression( type.Name );
                    }
                }

                return base.VisitInvocationExpression( node );
            }

            public override SyntaxNode VisitIdentifierName( IdentifierNameSyntax node )
            {
                var symbol = this._parent._syntaxTreeAnnotationMap.GetSymbol( node );

                if ( node.Identifier.IsKind( SyntaxKind.IdentifierToken )
                     && node is { IsVar: false, Parent: not (QualifiedNameSyntax or AliasQualifiedNameSyntax) } &&
                     !(node.Parent.IsKind( SyntaxKind.SimpleMemberAccessExpression ) && node.Parent is MemberAccessExpressionSyntax memberAccessExpressionSyntax
                                                                                     && node == memberAccessExpressionSyntax.Name) )
                {
                    // Fully qualifies simple identifiers.
                    if ( symbol?.Kind is SymbolKind.Namespace or SymbolKind.NamedType or SymbolKind.ArrayType or SymbolKind.PointerType
                             or SymbolKind.DynamicType or SymbolKind.TypeParameter or SymbolKind.ErrorType
                         && symbol is INamespaceOrTypeSymbol namespaceOrType )
                    {
                        return node.CopyAnnotationsTo( this._syntaxGenerator.TypeOrNamespace( namespaceOrType ).WithTriviaFrom( node ) );
                    }
                    else if ( symbol is { IsStatic: true } && !node.Parent.IsKind( SyntaxKind.SimpleMemberAccessExpression )
                                                           && !node.Parent.IsKind( SyntaxKind.AliasQualifiedName ) )
                    {
                        switch ( symbol.Kind )
                        {
                            case SymbolKind.Field:
                            case SymbolKind.Property:
                            case SymbolKind.Event:
                            case SymbolKind.Method:
                                // We have an access to a field or method with a "using static", or a non-qualified static member access.
                                // Move leading trivia from the identifier to the type qualifier to avoid splitting comments
                                return
                                    node.CopyAnnotationsTo(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                this._syntaxGenerator.TypeOrNamespace( symbol.ContainingType ),
                                                SyntaxFactoryEx.WellKnownIdentifierName( node.Identifier )
                                                    .WithoutLeadingTrivia()
                                                    .WithTrailingTrivia( node.GetTrailingTrivia() ) ) )
                                        .WithLeadingTrivia( node.GetLeadingTrivia() );
                        }
                    }
                }

                return node;
            }
        }
    }
}