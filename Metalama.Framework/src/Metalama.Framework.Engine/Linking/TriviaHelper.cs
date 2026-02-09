// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

internal static class TriviaHelper
{
    /// <summary>
    /// Transfers XML documentation comment trivia from a property declaration to a backing field declaration.
    /// Only <c>SingleLineDocumentationCommentTrivia</c> and <c>MultiLineDocumentationCommentTrivia</c> are moved;
    /// all other leading trivia remains on the property declaration.
    /// </summary>
    public static (FieldDeclarationSyntax Field, PropertyDeclarationSyntax Property) TransferDocumentationTrivia(
        FieldDeclarationSyntax backingField,
        PropertyDeclarationSyntax propertyDeclaration,
        SyntaxGenerationOptions options )
    {
        var leadingTrivia = propertyDeclaration.GetLeadingTrivia();

        if ( !leadingTrivia.ShouldBePreserved( options ) )
        {
            return (backingField, propertyDeclaration);
        }

        var docCommentTrivia = new List<SyntaxTrivia>();
        var remainingTrivia = new List<SyntaxTrivia>();

        foreach ( var trivia in leadingTrivia )
        {
            if ( trivia.IsKind( SyntaxKind.SingleLineDocumentationCommentTrivia )
                 || trivia.IsKind( SyntaxKind.MultiLineDocumentationCommentTrivia ) )
            {
                docCommentTrivia.Add( trivia );
            }
            else
            {
                remainingTrivia.Add( trivia );
            }
        }

        if ( docCommentTrivia.Count == 0 )
        {
            return (backingField, propertyDeclaration);
        }

        var fieldLeadingTrivia = new SyntaxTriviaList( docCommentTrivia );

        backingField = backingField.WithRequiredLeadingTrivia(
            fieldLeadingTrivia.AddRange( backingField.GetLeadingTrivia() ) );

        propertyDeclaration = propertyDeclaration.WithRequiredLeadingTrivia( remainingTrivia );

        return (backingField, propertyDeclaration);
    }
}
