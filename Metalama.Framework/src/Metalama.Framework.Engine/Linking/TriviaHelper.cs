// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

internal static class TriviaHelper
{
    /// <summary>
    /// Extracts XML documentation comment trivia from a field declaration.
    /// Returns the documentation trivia list, or an empty list if none is found.
    /// </summary>
    public static SyntaxTriviaList GetDocumentationTrivia( FieldDeclarationSyntax fieldDeclaration )
    {
        var leadingTrivia = fieldDeclaration.GetLeadingTrivia();
        var docCommentTrivia = new List<SyntaxTrivia>();

        foreach ( var trivia in leadingTrivia )
        {
            if ( trivia.IsKind( SyntaxKind.SingleLineDocumentationCommentTrivia )
                 || trivia.IsKind( SyntaxKind.MultiLineDocumentationCommentTrivia ) )
            {
                docCommentTrivia.Add( trivia );
            }
        }

        return new SyntaxTriviaList( docCommentTrivia );
    }

    /// <summary>
    /// Extracts non-documentation comment trivia (regular comments and directives) from a field declaration.
    /// Returns the trivia list with associated whitespace, or an empty list if none is found.
    /// When a field is promoted to a property, these trivia should stay with the backing field
    /// (they are associated with the private implementation detail, not the public member).
    /// </summary>
    public static SyntaxTriviaList GetNonDocumentationTrivia( FieldDeclarationSyntax fieldDeclaration )
    {
        var leadingTrivia = fieldDeclaration.GetLeadingTrivia();
        var result = new List<SyntaxTrivia>();

        for ( var i = 0; i < leadingTrivia.Count; i++ )
        {
            var trivia = leadingTrivia[i];

            if ( trivia.IsKind( SyntaxKind.SingleLineCommentTrivia )
                 || trivia.IsKind( SyntaxKind.MultiLineCommentTrivia )
                 || trivia.IsDirective )
            {
                // Include the whitespace trivia before the comment for proper indentation.
                if ( result.Count == 0 && i > 0 && leadingTrivia[i - 1].IsKind( SyntaxKind.WhitespaceTrivia ) )
                {
                    result.Add( leadingTrivia[i - 1] );
                }

                result.Add( trivia );

                // Include the end-of-line trivia after the comment.
                if ( i + 1 < leadingTrivia.Count && leadingTrivia[i + 1].IsKind( SyntaxKind.EndOfLineTrivia ) )
                {
                    result.Add( leadingTrivia[i + 1] );
                }
            }
        }

        return new SyntaxTriviaList( result );
    }

    /// <summary>
    /// Adds documentation trivia to the leading trivia of a member declaration.
    /// The documentation trivia is prepended before the member's existing leading trivia.
    /// </summary>
    public static T WithDocumentationTrivia<T>( T member, SyntaxTriviaList documentationTrivia )
        where T : MemberDeclarationSyntax
    {
        if ( documentationTrivia.Count == 0 )
        {
            return member;
        }

        var existingTrivia = member.GetLeadingTrivia();

        return member.WithRequiredLeadingTrivia( documentationTrivia.AddRange( existingTrivia ) );
    }

}
