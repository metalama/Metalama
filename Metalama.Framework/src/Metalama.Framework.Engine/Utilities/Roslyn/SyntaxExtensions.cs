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
        => node.WithSimplifierAnnotationIfNecessary( context.Options );

    internal static TNode WithSimplifierAnnotationIfNecessary<TNode>( this TNode node, SyntaxGenerationOptions options )
        where TNode : SyntaxNode
    {
        if ( !options.AddFormattingAnnotations )
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
        if ( !context.Options.TriviaMatters )
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

    internal static TNode WithOptionalTrailingTrivia<TNode>( this TNode node, SyntaxTriviaList trailingTrivia, SyntaxGenerationContext context )
        where TNode : SyntaxNode
        => node.WithOptionalTrailingTrivia( trailingTrivia, context.Options );

    // Resharper disable once UnusedMember.Global
    internal static SyntaxToken WithOptionalTrailingTrivia( this SyntaxToken token, SyntaxTriviaList trailingTrivia, SyntaxGenerationOptions options )
    {
        if ( !options.TriviaMatters && !trailingTrivia.ContainsDirectives() )
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
        if ( !options.TriviaMatters && !leadingTrivia.ContainsDirectives() && !trailingTrivia.ContainsDirectives() )
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
    public static bool ContainsGlobalAttributes( this SyntaxTree tree ) => tree.GetCompilationUnitRoot().AttributeLists.Any( list => list.Attributes.Any() );

    internal static ExpressionSyntax IgnoreSuppressNullWarning( this ExpressionSyntax expression )
        => expression switch
        {
            PostfixUnaryExpressionSyntax postfix when postfix.IsKind( SyntaxKind.SuppressNullableWarningExpression ) => postfix.Operand,
            _ => expression
        };

    /// <summary>
    /// Finds a corresponding node in a different syntax tree based on its position in the child collection of parents.
    /// This works by building a path from the source node to the source root, then navigating the same path
    /// from the target root to find the equivalent node.
    /// </summary>
    /// <param name="sourceNode">The node to find an equivalent for in the target tree.</param>
    /// <param name="sourceRoot">The root of the source tree (must be an ancestor of sourceNode).</param>
    /// <param name="targetRoot">The root of the target tree to search in.</param>
    /// <param name="targetNode">When this method returns, contains the equivalent node in the target tree if found; otherwise, null.</param>
    /// <returns>True if the equivalent node was found; otherwise, false.</returns>
    internal static bool TryFindNodeByPosition(
        SyntaxNode sourceNode,
        SyntaxNode sourceRoot,
        SyntaxNode targetRoot,
        out SyntaxNode? targetNode )
    {
        // Build a path from sourceNode to sourceRoot as a stack of (child index, syntax kind) pairs
        var path = new Stack<(int ChildIndex, SyntaxKind Kind)>();
        var current = sourceNode;

        while ( current != sourceRoot )
        {
            var parent = current.Parent;

            if ( parent == null )
            {
                // sourceNode is not a descendant of sourceRoot
                targetNode = null;

                return false;
            }

            // Find the index of current node in its parent's children
            var children = parent.ChildNodesAndTokens();
            var childIndex = -1;

            for ( var i = 0; i < children.Count; i++ )
            {
                if ( children[i].AsNode() == current )
                {
                    childIndex = i;

                    break;
                }
            }

            if ( childIndex < 0 )
            {
                // This should never happen, but handle it gracefully
                targetNode = null;

                return false;
            }

            path.Push( (childIndex, current.Kind()) );
            current = parent;
        }

        // Now navigate from targetRoot using the path in reverse order
        current = targetRoot;

        while ( path.Count > 0 )
        {
            var (childIndex, expectedKind) = path.Pop();
            var children = current.ChildNodesAndTokens();

            if ( childIndex >= children.Count )
            {
                // The target tree structure doesn't match
                targetNode = null;

                return false;
            }

            var childNodeOrToken = children[childIndex];

            if ( !childNodeOrToken.IsNode )
            {
                // Expected a node but found a token
                targetNode = null;

                return false;
            }

            current = childNodeOrToken.AsNode()!;

            if ( !current.IsKind( expectedKind ) )
            {
                // The syntax kind doesn't match - trees are structurally different
                targetNode = null;

                return false;
            }
        }

        targetNode = current;

        return true;
    }
}