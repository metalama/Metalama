// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

public static class SyntaxExtensions
{
    internal static MemberDeclarationSyntax FindMemberDeclaration( this SyntaxNode node )
        => node.FindMemberDeclarationOrNull()
           ?? throw new AssertionFailedException( $"The {node.Kind()} at '{node.GetLocation()}' is not the descendant of a member declaration." );

    private static MemberDeclarationSyntax? FindMemberDeclarationOrNull( this SyntaxNode node )
    {
        var current = node;

        while ( current != null )
        {
            if ( current.Kind() is SyntaxKind.MethodDeclaration or SyntaxKind.ConstructorDeclaration or SyntaxKind.DestructorDeclaration
                     or SyntaxKind.OperatorDeclaration or SyntaxKind.ConversionOperatorDeclaration
                     or SyntaxKind.PropertyDeclaration or SyntaxKind.IndexerDeclaration or SyntaxKind.EventDeclaration
                     or SyntaxKind.FieldDeclaration or SyntaxKind.EventFieldDeclaration
                     or SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration
                     or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration or SyntaxKind.EnumDeclaration
                     or SyntaxKind.DelegateDeclaration or SyntaxKind.ExtensionBlockDeclaration
#if ROSLYN_5_11_0_OR_GREATER
                     or SyntaxKind.UnionDeclaration
#endif
                     or SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration
                     or SyntaxKind.IncompleteMember or SyntaxKind.GlobalStatement
                 && current is MemberDeclarationSyntax memberDeclaration )
            {
                return memberDeclaration;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Find the parent node that declares an <see cref="ISymbol"/>, but not a local variable or a function.
    /// </summary>
    public static SyntaxNode? FindSymbolDeclaringNode( this SyntaxNode node )
    {
        var current = node;

        while ( current != null )
        {
            if ( (current.Kind() is SyntaxKind.MethodDeclaration or SyntaxKind.ConstructorDeclaration or SyntaxKind.DestructorDeclaration
                      or SyntaxKind.OperatorDeclaration or SyntaxKind.ConversionOperatorDeclaration
                      or SyntaxKind.PropertyDeclaration or SyntaxKind.IndexerDeclaration or SyntaxKind.EventDeclaration
                      or SyntaxKind.FieldDeclaration or SyntaxKind.EventFieldDeclaration
                      or SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration
                      or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration or SyntaxKind.EnumDeclaration
                      or SyntaxKind.DelegateDeclaration or SyntaxKind.ExtensionBlockDeclaration
#if ROSLYN_5_11_0_OR_GREATER
                      or SyntaxKind.UnionDeclaration
#endif
                      or SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration
                      or SyntaxKind.IncompleteMember or SyntaxKind.GlobalStatement
                  && current is MemberDeclarationSyntax)
                 || (current.IsKind( SyntaxKind.VariableDeclarator ) && current is VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax }) )
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }

    internal static bool IsAutoPropertyDeclaration( this PropertyDeclarationSyntax propertyDeclaration )
        => propertyDeclaration.ExpressionBody == null
           && propertyDeclaration.AccessorList?.Accessors.All( x => x.Body == null && x.ExpressionBody == null ) == true
           && propertyDeclaration.Modifiers.All( x => x.Kind() is not (SyntaxKind.AbstractKeyword or SyntaxKind.PartialKeyword) );

    internal static bool HasSetterAccessorDeclaration( this PropertyDeclarationSyntax propertyDeclaration )
        => propertyDeclaration.AccessorList != null
           && propertyDeclaration.AccessorList.Accessors.Any( a => a.IsKind( SyntaxKind.SetAccessorDeclaration ) );

    internal static bool IsAccessModifierKeyword( this SyntaxToken token ) => SyntaxFacts.IsAccessibilityModifier( token.Kind() );

    /// <summary>
    /// Unwraps parentheses from an expression, walking down through children.
    /// </summary>
    /// <seealso cref="Metalama.Framework.Engine.Linking.Inlining.InlinerHelper.SkipParenthesizedExpressionAncestors"/>
    internal static ExpressionSyntax RemoveParenthesis( this ExpressionSyntax node )
        => node.Kind() switch
        {
            SyntaxKind.ParenthesizedExpression when node is ParenthesizedExpressionSyntax parenthesized => parenthesized.Expression.RemoveParenthesis(),
            _ => node
        };

    /// <summary>
    /// Unwraps parentheses and null-forgiving operators from an expression, walking down through children.
    /// </summary>
    /// <seealso cref="Metalama.Framework.Engine.Linking.Inlining.InlinerHelper.SkipParenthesizedExpressionAncestors"/>
    internal static ExpressionSyntax RemoveParenthesisAndNullForgiving( this ExpressionSyntax node )
        => node switch
        {
            { SyntaxKind: SyntaxKind.ParenthesizedExpression } and ParenthesizedExpressionSyntax parenthesized
                => parenthesized.Expression.RemoveParenthesisAndNullForgiving(),
            { SyntaxKind: SyntaxKind.SuppressNullableWarningExpression } and PostfixUnaryExpressionSyntax nullForgiving
                => nullForgiving.Operand.RemoveParenthesisAndNullForgiving(),
            _ => node
        };

    internal static TypeDeclarationSyntax? GetDeclaringType( this SyntaxNode node )
        => node.Kind() switch
        {
            SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration
                or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration or SyntaxKind.EnumDeclaration
                or SyntaxKind.ExtensionBlockDeclaration
#if ROSLYN_5_11_0_OR_GREATER
                or SyntaxKind.UnionDeclaration
#endif
                when node is TypeDeclarationSyntax type => type,
            _ => node.Parent?.GetDeclaringType()
        };

    internal static bool IsNameOf( this InvocationExpressionSyntax node )
        => node.Expression.IsKind( SyntaxKind.NameOfKeyword ) ||
           (node.Expression.IsKind( SyntaxKind.IdentifierName ) && node.Expression is IdentifierNameSyntax identifierName && string.Equals(
               identifierName.Identifier.Text,
               "nameof",
               StringComparison.Ordinal ));

    internal static TypeSyntax GetNamespaceOrType( this UsingDirectiveSyntax usingDirective ) => usingDirective.NamespaceOrType;

    internal static ParameterListSyntax? GetParameterList( this TypeDeclarationSyntax typeDeclaration ) => typeDeclaration.ParameterList;

    internal static TNode NormalizeWhitespaceIfNecessary<TNode>( this TNode node, SyntaxGenerationContext context )
        where TNode : SyntaxNode
    {
        if ( !context.Options.WillBeTextualized )
        {
            return node;
        }

#pragma warning disable LAMA0830 // NormalizeWhitespace is expensive.
        return node.NormalizeWhitespace( elasticTrivia: true, eol: context.EndOfLine );
#pragma warning restore LAMA0830
    }

    /// <summary>
    /// The maximal depth of a node that <see cref="CanNormalizeWhitespace"/> accepts.
    /// </summary>
    private const int _maxNormalizedDepth = 100;

    /// <summary>
    /// Determines whether the depth of the given node is small enough for
    /// <see cref="Microsoft.CodeAnalysis.SyntaxNodeExtensions.NormalizeWhitespace{TNode}(TNode, string, string, bool)"/> to process it safely.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The normalizer of Roslyn is recursive. It calls itself for each ancestor of a node without checking the remaining
    /// stack, so it can cause a <see cref="StackOverflowException"/> on a deep node (see #2083). Such a node typically
    /// comes from user code.
    /// </para>
    /// <para>
    /// The method uses an explicit stack instead of a recursion, so that it can process a deep node. It returns as soon as
    /// it finds a descendant that is too deep.
    /// </para>
    /// </remarks>
    internal static bool CanNormalizeWhitespace( this SyntaxNode node )
    {
        var stack = new Stack<(SyntaxNode Node, int Depth)>();
        stack.Push( (node, 0) );

        while ( stack.Count > 0 )
        {
            var (current, depth) = stack.Pop();

            if ( depth > _maxNormalizedDepth )
            {
                return false;
            }

            foreach ( var child in current.ChildNodes() )
            {
                stack.Push( (child, depth + 1) );
            }
        }

        return true;
    }

    /// <summary>
    /// Adds a space after each token of the given node that the lexer would merge with the next token, because no trivia
    /// separates them. The other trivia do not change.
    /// </summary>
    /// <remarks>
    /// This method is an alternative to <see cref="Microsoft.CodeAnalysis.SyntaxNodeExtensions.NormalizeWhitespace{TNode}(TNode, string, string, bool)"/>
    /// for a node that <see cref="CanNormalizeWhitespace"/> rejects. It enumerates the tokens without a recursion and
    /// rewrites the node with a <see cref="SafeSyntaxRewriter"/>, so that it can process a deep node (see #2083).
    /// </remarks>
    internal static TNode AddMissingTokenSeparators<TNode>( this TNode node )
        where TNode : SyntaxNode
    {
        var tokensRequiringSeparator = new HashSet<SyntaxToken>();
        SyntaxToken? previousToken = null;

        foreach ( var token in node.DescendantTokens() )
        {
            if ( previousToken != null && RequiresSeparator( previousToken.Value, token ) )
            {
                tokensRequiringSeparator.Add( previousToken.Value );
            }

            previousToken = token;
        }

        if ( tokensRequiringSeparator.Count == 0 )
        {
            return node;
        }

        return (TNode) new AddTokenSeparatorRewriter( tokensRequiringSeparator ).Visit( node ).AssertNotNull();
    }

    /// <summary>
    /// Returns the text of the given node, like <see cref="SyntaxNode.ToString"/>, but with a space between two tokens that
    /// the lexer would merge because no trivia separates them.
    /// </summary>
    /// <remarks>
    /// This method is an alternative to <see cref="Microsoft.CodeAnalysis.SyntaxNodeExtensions.NormalizeWhitespace{TNode}(TNode, string, string, bool)"/>
    /// for a node that <see cref="CanNormalizeWhitespace"/> rejects. It enumerates the tokens without a recursion, so that it
    /// can process a deep node (see #2083). The trivia and the text of the tokens, including the content of interpolated
    /// strings, do not change.
    /// </remarks>
    internal static string ToStringWithTokenSeparators( this SyntaxNode node )
    {
        var stringBuilder = new StringBuilder();
        SyntaxToken? previousToken = null;

        foreach ( var token in node.DescendantTokens() )
        {
            if ( previousToken != null )
            {
                stringBuilder.Append( previousToken.Value.TrailingTrivia.ToFullString() );

                if ( RequiresSeparator( previousToken.Value, token ) )
                {
                    stringBuilder.Append( ' ' );
                }

                stringBuilder.Append( token.LeadingTrivia.ToFullString() );
            }

            stringBuilder.Append( token.Text );

            previousToken = token;
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Determines whether the lexer would read the two given adjacent tokens as a different sequence of tokens, because no
    /// trivia separates them. For instance, <c>x</c> and <c>is</c> would be read as the identifier <c>xis</c>,
    /// and <c>+</c> and <c>+</c> would be read as <c>++</c>.
    /// </summary>
    private static bool RequiresSeparator( SyntaxToken previousToken, SyntaxToken nextToken )
    {
        if ( previousToken.Span.End != previousToken.FullSpan.End || nextToken.Span.Start != nextToken.FullSpan.Start
                                                                   || previousToken.Span.Length == 0 || nextToken.Span.Length == 0 )
        {
            // There is a trivia between the tokens, or one of the tokens is empty.
            return false;
        }

        if ( previousToken.Kind() is SyntaxKind.InterpolatedStringStartToken or SyntaxKind.InterpolatedVerbatimStringStartToken
                or SyntaxKind.InterpolatedSingleLineRawStringStartToken or SyntaxKind.InterpolatedMultiLineRawStringStartToken
                or SyntaxKind.InterpolatedStringTextToken
            || nextToken.Kind() is SyntaxKind.InterpolatedStringTextToken or SyntaxKind.InterpolatedStringEndToken
                or SyntaxKind.InterpolatedRawStringEndToken )
        {
            // The content of an interpolated string must not change.
            return false;
        }

        if ( previousToken.Kind() is SyntaxKind.InterpolatedStringEndToken or SyntaxKind.InterpolatedRawStringEndToken )
        {
            // The end of an interpolated string cannot be merged with the next token. It cannot be lexed alone because the
            // lexer would read it as the start of a string.
            return false;
        }

        var previousText = previousToken.Text;
        var lexedToken = ParseToken( previousText + nextToken.Text );

        return lexedToken.Text != previousText;
    }

    private sealed class AddTokenSeparatorRewriter : SafeSyntaxRewriter
    {
        private readonly HashSet<SyntaxToken> _tokensRequiringSeparator;

        public AddTokenSeparatorRewriter( HashSet<SyntaxToken> tokensRequiringSeparator )
        {
            this._tokensRequiringSeparator = tokensRequiringSeparator;
        }

        // The space is required for the code to be valid, so it must be added even when the options do not require trivia.
#pragma warning disable LAMA0832 // Avoid WithLeadingTrivia and WithTrailingTrivia calls.
        public override SyntaxToken VisitToken( SyntaxToken token )
            => this._tokensRequiringSeparator.Contains( token ) ? token.WithTrailingTrivia( token.TrailingTrivia.Add( Space ) ) : token;
#pragma warning restore LAMA0832
    }

    internal static TNode WithSimplifierAnnotationIfNecessary<TNode>( this TNode node, SyntaxGenerationContext context )
        where TNode : SyntaxNode
        => node.WithSimplifierAnnotationIfNecessary( context.Options );

    internal static TNode WithSimplifierAnnotationIfNecessary<TNode>( this TNode node, SyntaxGenerationOptions options )
        where TNode : SyntaxNode
    {
        if ( !options.WillBeFormatted )
        {
            return node;
        }

        return node.WithSimplifierAnnotation();
    }

    private static bool ContainsDirectives( this SyntaxTriviaList trivias )
    {
        // PERF: `trivias.Any(t => t.IsDirective)` would allocate — the predicate materializes
        // a delegate, and calling `Any` via IEnumerable<T> boxes the struct enumerator.
        // A hand-rolled foreach over `SyntaxTriviaList` uses its struct enumerator directly.

        foreach ( var trivia in trivias )
        {
            if ( trivia.IsDirective )
            {
                return true;
            }
        }

        return false;
    }

#pragma warning disable LAMA0832 // Avoid WithLeadingTrivia and WithTrailingTrivia calls.

    internal static TNode WithOptionalLeadingTrivia<TNode>( this TNode node, SyntaxTriviaList leadingTrivia, SyntaxGenerationOptions options )
        where TNode : SyntaxNode
    {
        if ( !options.WillBeTextualized && !leadingTrivia.ContainsDirectives() )
        {
            return node;
        }

        return node.WithLeadingTrivia( leadingTrivia );
    }

    internal static TNode WithOptionalLeadingTrivia<TNode>( this TNode node, SyntaxTriviaList leadingTrivia, SyntaxGenerationContext context )
        where TNode : SyntaxNode
        => node.WithOptionalLeadingTrivia( leadingTrivia, context.Options );

    internal static TNode WithRequiredLeadingTrivia<TNode>( this TNode node, IEnumerable<SyntaxTrivia> leadingTrivia )
        where TNode : SyntaxNode
        => node.WithLeadingTrivia( TriviaList( leadingTrivia ) );

    internal static TNode WithRequiredLeadingTrivia<TNode>( this TNode node, SyntaxTriviaList leadingTrivia )
        where TNode : SyntaxNode
        => node.WithLeadingTrivia( leadingTrivia );

    internal static SyntaxToken WithRequiredLeadingTrivia( this SyntaxToken token, IEnumerable<SyntaxTrivia> leadingTrivia )
        => token.WithLeadingTrivia( TriviaList( leadingTrivia ) );

    internal static SyntaxToken WithRequiredLeadingTrivia( this SyntaxToken token, SyntaxTriviaList leadingTrivia ) => token.WithLeadingTrivia( leadingTrivia );

    internal static TNode WithOptionalLeadingLineFeed<TNode>(
        this TNode node,
        SyntaxGenerationContext context )
        where TNode : SyntaxNode
    {
        if ( !context.Options.WillBeTextualized )
        {
            return node;
        }

        return node.WithLeadingTrivia( node.GetLeadingTrivia().Add( context.ElasticEndOfLineTrivia ) );
    }

    internal static TNode WithRequiredLeadingLineFeed<TNode>(
        this TNode node,
        SyntaxGenerationContext context )
        where TNode : SyntaxNode
        => node.WithLeadingTrivia( node.GetLeadingTrivia().Add( context.ElasticEndOfLineTrivia ) );

    internal static TNode WithOptionalLeadingAndTrailingLineFeed<TNode>(
        this TNode node,
        SyntaxGenerationContext context )
        where TNode : SyntaxNode
    {
        if ( !context.Options.WillBeTextualized )
        {
            return node;
        }

        return node.WithLeadingTrivia( node.GetLeadingTrivia().Add( context.ElasticEndOfLineTrivia ) )
            .WithTrailingTrivia( node.GetTrailingTrivia().Add( context.ElasticEndOfLineTrivia ) );
    }

    internal static TNode WithOptionalTrailingLineFeed<TNode>(
        this TNode node,
        SyntaxGenerationContext context )
        where TNode : SyntaxNode
    {
        if ( !context.Options.WillBeTextualized )
        {
            return node;
        }

        return node.WithTrailingTrivia( node.GetTrailingTrivia().Add( context.ElasticEndOfLineTrivia ) );
    }

    internal static SyntaxToken WithOptionalTrailingLineFeed(
        this SyntaxToken node,
        SyntaxGenerationContext context )
    {
        if ( !context.Options.WillBeTextualized )
        {
            return node;
        }

        return node.WithTrailingTrivia( node.TrailingTrivia.Add( context.ElasticEndOfLineTrivia ) );
    }

    internal static SyntaxToken WithRequiredTrailingLineFeed(
        this SyntaxToken node,
        SyntaxGenerationContext context )
        => node.WithTrailingTrivia( node.TrailingTrivia.Add( context.ElasticEndOfLineTrivia ) );

    internal static SyntaxToken WithRequiredLeadingLineFeed(
        this SyntaxToken node,
        SyntaxGenerationContext context )
        => node.WithLeadingTrivia( node.LeadingTrivia.Add( context.ElasticEndOfLineTrivia ) );

    internal static TNode StructuredTriviaWithRequiredTrailingLineFeed<TNode>(
        this TNode node,
        SyntaxGenerationContext context )
        where TNode : StructuredTriviaSyntax
        => node.WithTrailingTrivia( node.GetTrailingTrivia().Add( context.ElasticEndOfLineTrivia ) );

    internal static TNode StructuredTriviaWithRequiredLeadingLineFeed<TNode>(
        this TNode node,
        SyntaxGenerationContext context )
        where TNode : StructuredTriviaSyntax
        => node.WithLeadingTrivia( node.GetLeadingTrivia().Add( context.ElasticEndOfLineTrivia ) );

    internal static SyntaxTriviaList AddOptionalLineFeed(
        this SyntaxTriviaList list,
        SyntaxGenerationContext context )
    {
        if ( !context.Options.WillBeTextualized )
        {
            return list;
        }

        return list.Add( context.ElasticEndOfLineTrivia );
    }

    internal static TNode WithOptionalLeadingTrivia<TNode>( this TNode node, SyntaxTrivia leadingTrivia, SyntaxGenerationOptions options )
        where TNode : SyntaxNode
        => node.WithOptionalLeadingTrivia( new SyntaxTriviaList( leadingTrivia ), options );

    internal static TNode WithOptionalTrailingTrivia<TNode>( this TNode node, SyntaxTriviaList trailingTrivia, SyntaxGenerationOptions options )
        where TNode : SyntaxNode
    {
        if ( !options.WillBeTextualized && !trailingTrivia.ContainsDirectives() )
        {
            return node;
        }

        return node.WithTrailingTrivia( trailingTrivia );
    }

    internal static TNode WithOptionalTrailingTrivia<TNode>( this TNode node, SyntaxTriviaList trailingTrivia, SyntaxGenerationContext context )
        where TNode : SyntaxNode
        => node.WithOptionalTrailingTrivia( trailingTrivia, context.Options );

    internal static SyntaxToken WithOptionalTrailingTrivia( this SyntaxToken token, SyntaxTriviaList trailingTrivia, SyntaxGenerationOptions options )
    {
        if ( !options.WillBeTextualized && !trailingTrivia.ContainsDirectives() )
        {
            return token;
        }

        return token.WithTrailingTrivia( trailingTrivia );
    }

    internal static TNode WithRequiredTrailingSpace<TNode>( this TNode node )
        where TNode : SyntaxNode
        => node.WithRequiredTrailingTrivia( SyntaxFactoryEx.ElasticSpaceTriviaList );

    internal static TNode WithRequiredTrailingTrivia<TNode>( this TNode node, SyntaxTriviaList trailingTrivia )
        where TNode : SyntaxNode
        => node.WithTrailingTrivia( trailingTrivia );

    internal static SyntaxToken WithRequiredTrailingTrivia( this SyntaxToken token, SyntaxTriviaList trailingTrivia )
        => token.WithTrailingTrivia( trailingTrivia );

    internal static TNode WithOptionalTrailingTrivia<TNode>( this TNode node, SyntaxTrivia trailingTrivia, SyntaxGenerationOptions options )
        where TNode : SyntaxNode
        => node.WithOptionalTrailingTrivia( new SyntaxTriviaList( trailingTrivia ), options );

    internal static SyntaxToken WithOptionalTrailingTrivia( this SyntaxToken token, SyntaxTriviaList trailingTrivia, bool preserveTrivia )
    {
        if ( !preserveTrivia && !trailingTrivia.ContainsDirectives() )
        {
            return token;
        }

        return token.WithTrailingTrivia( trailingTrivia );
    }

    internal static TNode WithOptionalTrivia<TNode>(
        this TNode node,
        SyntaxTriviaList leadingTrivia,
        SyntaxTriviaList trailingTrivia,
        SyntaxGenerationOptions options )
        where TNode : SyntaxNode
    {
        if ( !options.WillBeTextualized && !leadingTrivia.ContainsDirectives() && !trailingTrivia.ContainsDirectives() )
        {
            return node;
        }

        return node.WithLeadingTrivia( leadingTrivia ).WithTrailingTrivia( trailingTrivia );
    }

    internal static TNode WithTriviaFromIfNecessary<TNode>( this TNode node, SyntaxNode fromNode, SyntaxGenerationOptions options )
        where TNode : SyntaxNode
        => node.WithOptionalTrivia( fromNode.GetLeadingTrivia(), fromNode.GetTrailingTrivia(), options );

    internal static TNode WithTriviaFromIfNecessary<TNode>( this TNode node, SyntaxNode fromNode, SyntaxGenerationContext context )
        where TNode : SyntaxNode
        => node.WithTriviaFromIfNecessary( fromNode, context.Options );

    internal static bool ShouldBePreserved( this SyntaxTriviaList trivia, SyntaxGenerationOptions options )
        => options.WillBeTextualized || trivia.ContainsDirectives();

    internal static bool ShouldBePreserved( this IEnumerable<SyntaxTrivia> trivia, SyntaxGenerationOptions options )
        => options.WillBeTextualized || trivia.Any( t => t.IsDirective );

    internal static bool ShouldTriviaBePreserved( this SyntaxNodeOrToken nodeOrToken, SyntaxGenerationOptions options )
        => options.WillBeTextualized || nodeOrToken.ContainsDirectives;

    internal static TNode AddTriviaFromIfNecessary<TNode>( this TNode node, SyntaxNode fromNode, SyntaxGenerationOptions options )
        where TNode : SyntaxNode
    {
        var fromLeading = fromNode.GetLeadingTrivia();
        var fromTrailing = fromNode.GetTrailingTrivia();

        if ( !options.WillBeTextualized && !fromLeading.ContainsDirectives() && !fromTrailing.ContainsDirectives() )
        {
            return node;
        }

        return node
            .WithLeadingTrivia( fromLeading.AddRange( node.GetLeadingTrivia() ) )
            .WithTrailingTrivia( node.GetTrailingTrivia().AddRange( fromTrailing ) );
    }
#pragma warning restore LAMA0832

    /// <summary>
    /// Returns true when the tree contains assembly or module attributes.
    /// </summary>
    public static bool ContainsGlobalAttributes( this SyntaxTree tree ) => tree.GetCompilationUnitRoot().AttributeLists.Any( list => list.Attributes.Any() );

    internal static ExpressionSyntax IgnoreSuppressNullWarning( this ExpressionSyntax expression )
        => expression.Kind() switch
        {
            SyntaxKind.SuppressNullableWarningExpression when expression is PostfixUnaryExpressionSyntax postfix => postfix.Operand,
            _ => expression
        };
}