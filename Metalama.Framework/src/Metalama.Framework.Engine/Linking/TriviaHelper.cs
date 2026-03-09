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
    /// Extracts comment trivia (regular comments and XML documentation comments) from a field declaration.
    /// Returns the comment trivia list (including associated whitespace), or an empty list if none is found.
    /// </summary>
    public static SyntaxTriviaList GetCommentTrivia( FieldDeclarationSyntax fieldDeclaration )
    {
        var leadingTrivia = fieldDeclaration.GetLeadingTrivia();
        var commentTrivia = new List<SyntaxTrivia>();

        for ( var i = 0; i < leadingTrivia.Count; i++ )
        {
            var trivia = leadingTrivia[i];

            if ( trivia.IsKind( SyntaxKind.SingleLineCommentTrivia )
                 || trivia.IsKind( SyntaxKind.MultiLineCommentTrivia )
                 || trivia.IsKind( SyntaxKind.SingleLineDocumentationCommentTrivia )
                 || trivia.IsKind( SyntaxKind.MultiLineDocumentationCommentTrivia ) )
            {
                // Include the whitespace trivia before the comment for proper indentation.
                if ( commentTrivia.Count == 0 && i > 0 && leadingTrivia[i - 1].IsKind( SyntaxKind.WhitespaceTrivia ) )
                {
                    commentTrivia.Add( leadingTrivia[i - 1] );
                }

                commentTrivia.Add( trivia );

                // Include the end-of-line trivia after the comment.
                if ( i + 1 < leadingTrivia.Count && leadingTrivia[i + 1].IsKind( SyntaxKind.EndOfLineTrivia ) )
                {
                    commentTrivia.Add( leadingTrivia[i + 1] );
                }
            }
        }

        return new SyntaxTriviaList( commentTrivia );
    }

    /// <summary>
    /// Adds comment trivia to the leading trivia of a member declaration.
    /// The comment trivia is prepended before the member's existing leading trivia.
    /// </summary>
    public static T WithCommentTrivia<T>( T member, SyntaxTriviaList commentTrivia )
        where T : MemberDeclarationSyntax
    {
        if ( commentTrivia.Count == 0 )
        {
            return member;
        }

        var existingTrivia = member.GetLeadingTrivia();

        return member.WithRequiredLeadingTrivia( commentTrivia.AddRange( existingTrivia ) );
    }
}
