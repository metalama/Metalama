// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

public static class SyntaxExtensions
{
    internal static MemberDeclarationSyntax FindMemberDeclaration( this SyntaxNode node )
        => FindMemberDeclarationOrNull( node )
           ?? throw new AssertionFailedException( $"The {node.Kind()} at '{node.GetLocation()}' is not the descendant of a member declaration." );

    private static MemberDeclarationSyntax? FindMemberDeclarationOrNull( this SyntaxNode node )
    {
        var current = node;

        while ( current != null )
        {
            if ( current is MemberDeclarationSyntax memberDeclaration )
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
            if ( current is MemberDeclarationSyntax or VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax } )
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

    internal static ExpressionSyntax RemoveParenthesis( this ExpressionSyntax node )
        => node switch
        {
            ParenthesizedExpressionSyntax parenthesized => parenthesized.Expression.RemoveParenthesis(),
            _ => node
        };

    internal static TypeDeclarationSyntax? GetDeclaringType( this SyntaxNode node )
        => node switch
        {
            TypeDeclarationSyntax type => type,
            _ => node.Parent?.GetDeclaringType()
        };

    internal static bool IsNameOf( this InvocationExpressionSyntax node )
        => node.Expression.Kind() == SyntaxKind.NameOfKeyword ||
           (node.Expression is IdentifierNameSyntax identifierName && string.Equals( identifierName.Identifier.Text, "nameof", StringComparison.Ordinal ));

    internal static TypeSyntax GetNamespaceOrType( this UsingDirectiveSyntax usingDirective )
#if ROSLYN_4_8_0_OR_GREATER
        => usingDirective.NamespaceOrType;
#else
        => usingDirective.Name;
#endif

    internal static ParameterListSyntax? GetParameterList( this TypeDeclarationSyntax typeDeclaration )
    {
#if ROSLYN_4_8_0_OR_GREATER
        return typeDeclaration.ParameterList;
#else
        return typeDeclaration switch
        {
            RecordDeclarationSyntax record => record.ParameterList,
            _ => null
        };
#endif
    }

#if !ROSLYN_4_8_0_OR_GREATER
    internal static TypeDeclarationSyntax WithParameterList( this TypeDeclarationSyntax typeDeclaration, ParameterListSyntax? parameterList )
        => typeDeclaration is RecordDeclarationSyntax record ? record.WithParameterList( parameterList ) :
            parameterList == null ? typeDeclaration :
            throw new InvalidOperationException( $"Can't add parameter list to a non-record type before C# 12." );
#endif

    internal static TNode NormalizeWhitespaceIfNecessary<TNode>( this TNode node, SyntaxGenerationContext context )
        where TNode : SyntaxNode
    {
        if ( !context.Options.NormalizeWhitespace )
        {
            return node;
        }

#pragma warning disable LAMA0830 // NormalizeWhitespace is expensive.
        return node.NormalizeWhitespace( elasticTrivia: true, eol: context.EndOfLine );
#pragma warning restore LAMA0830
    }

    internal static TNode WithSimplifierAnnotationIfNecessary<TNode>( this TNode node, SyntaxGenerationContext context )
        where TNode : SyntaxNode
    {
        if ( !context.Options.AddFormattingAnnotations )
        {
            return node;
        }

        return node.WithSimplifierAnnotation();
    }

    private static bool ContainsDirectives( this SyntaxTriviaList trivias )
    {
        // PERF: Using trivias.Any( t => t.IsDirective ) would allocate, since SyntaxTriviaList is a struct.

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
        if ( !options.TriviaMatters && !leadingTrivia.ContainsDirectives() )
        {
            return node;
        }

        return node.WithLeadingTrivia( leadingTrivia );
    }

    internal static TNode WithRequiredLeadingTrivia<TNode>( this TNode node, IList<SyntaxTrivia> leadingTrivia )
        where TNode : SyntaxNode
        => node.WithLeadingTrivia( TriviaList( leadingTrivia ) );

    internal static TNode WithRequiredLeadingTrivia<TNode>( this TNode node, SyntaxTriviaList leadingTrivia )
        where TNode : SyntaxNode
        => node.WithLeadingTrivia( leadingTrivia );

    internal static SyntaxToken WithRequiredLeadingTrivia( this SyntaxToken token, IList<SyntaxTrivia> leadingTrivia )
        => token.WithLeadingTrivia( TriviaList( leadingTrivia ) );

    internal static SyntaxToken WithRequiredLeadingTrivia( this SyntaxToken token, SyntaxTriviaList leadingTrivia ) => token.WithLeadingTrivia( leadingTrivia );

    internal static TNode WithOptionalLeadingLineFeed<TNode>(
        this TNode node,
        SyntaxGenerationContext context )
        where TNode : SyntaxNode
    {
        if ( !context.Options.TriviaMatters )
        {
            return node;
        }

        return node.WithLeadingTrivia( node.GetLeadingTrivia().Add( context.ElasticEndOfLineTrivia ) );
    }

    internal static TNode WithOptionalLeadingAndTrailingLineFeed<TNode>(
        this TNode node,
        SyntaxGenerationContext context )
        where TNode : SyntaxNode
    {
        if ( !context.Options.TriviaMatters )
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
        if ( !context.Options.TriviaMatters )
        {
            return node;
        }

        return node.WithTrailingTrivia( node.GetTrailingTrivia().Add( context.ElasticEndOfLineTrivia ) );
    }

    internal static SyntaxToken WithOptionalTrailingLineFeed(
        this SyntaxToken node,
        SyntaxGenerationContext context )
    {
        if ( !context.Options.TriviaMatters )
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
        if ( !context.Options.NormalizeWhitespace )
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
        if ( !options.TriviaMatters && !trailingTrivia.ContainsDirectives() )
        {
            return node;
        }

        return node.WithTrailingTrivia( trailingTrivia );
    }

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
        if ( !options.TriviaMatters && !leadingTrivia.ContainsDirectives() && !trailingTrivia.ContainsDirectives() )
        {
            return node;
        }

        return node.WithLeadingTrivia( leadingTrivia ).WithTrailingTrivia( trailingTrivia );
    }

    internal static TNode WithTriviaFromIfNecessary<TNode>( this TNode node, SyntaxNode fromNode, SyntaxGenerationOptions options )
        where TNode : SyntaxNode
        => node.WithOptionalTrivia( fromNode.GetLeadingTrivia(), fromNode.GetTrailingTrivia(), options );

    internal static bool ShouldBePreserved( this SyntaxTriviaList trivia, SyntaxGenerationOptions options )
        => options.TriviaMatters || trivia.ContainsDirectives();

    internal static bool ShouldBePreserved( this IEnumerable<SyntaxTrivia> trivia, SyntaxGenerationOptions options )
        => options.TriviaMatters || trivia.Any( t => t.IsDirective );

    internal static bool ShouldTriviaBePreserved( this SyntaxNodeOrToken nodeOrToken, SyntaxGenerationOptions options )
        => options.TriviaMatters || nodeOrToken.ContainsDirectives;

    internal static TNode AddTriviaFromIfNecessary<TNode>( this TNode node, SyntaxNode fromNode, SyntaxGenerationOptions options )
        where TNode : SyntaxNode
    {
        var fromLeading = fromNode.GetLeadingTrivia();
        var fromTrailing = fromNode.GetTrailingTrivia();

        if ( !options.TriviaMatters && !fromLeading.ContainsDirectives() && !fromTrailing.ContainsDirectives() )
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
    public static bool ContainsGlobalAttributes( this SyntaxTree tree )
        => tree.GetCompilationUnitRoot().AttributeLists.Any( list => list.Attributes.Any() );
    
    public static ExpressionSyntax IgnoreSuppressNullWarning( this ExpressionSyntax expression )
        => expression switch
        {
            PostfixUnaryExpressionSyntax postfix when postfix.IsKind( SyntaxKind.SuppressNullableWarningExpression ) => postfix.Operand,
            _ => expression
        };
}