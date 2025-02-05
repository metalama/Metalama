// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking;

internal static class RewriterExtensions
{
    /* This is intended for processing trivia lists, but it is currently unused.
   public static SyntaxTriviaList VisitTriviaList( this CSharpSyntaxRewriter rewriter, SyntaxTriviaList triviaList )
   {

       var dirty = false;

#pragma warning disable IDE0059
       var list = new List<SyntaxTrivia>();

       foreach ( var trivia in triviaList )
       {
           // Not used anywhere yet.
           throw new AssertionFailedException( Justifications.CoverageMissing );

           var rewrittenTrivia = rewriter.VisitTrivia( trivia );

           list.Add( rewrittenTrivia );

           if ( !trivia.Equals( rewrittenTrivia ) )
           {
               dirty = true;
           }
       }

       if ( dirty )
       {
           // Not used anywhere yet.
           throw new AssertionFailedException( Justifications.CoverageMissing );

           // return TriviaList( list );
       }
       else
       {
           return triviaList;
       }


            return triviaList;
        }
         */

    public static PropertyDeclarationSyntax WithSynthesizedSetter(
        this PropertyDeclarationSyntax propertyDeclaration,
        SyntaxGenerationContext syntaxGenerationContext )
    {
        Invariant.Assert(
            propertyDeclaration
                .AccessorList.AssertNotNull()
                .Accessors
                .All( a => !a.IsKind( SyntaxKind.InitAccessorDeclaration ) && !a.IsKind( SyntaxKind.SetAccessorDeclaration ) ) );

        var isInit = syntaxGenerationContext.SupportsInitAccessors && !propertyDeclaration.Modifiers.Any( m => m.IsKind( SyntaxKind.StaticKeyword ) );

        return propertyDeclaration
            .WithAccessorList(
                propertyDeclaration.AccessorList.AssertNotNull()
                    .AddAccessors(
                        AccessorDeclaration(
                            isInit ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration,
                            List<AttributeListSyntax>(),
                            propertyDeclaration.Modifiers.Any( t => t.IsKind( SyntaxKind.PrivateKeyword ) )
                            || propertyDeclaration.Modifiers.All( t => !t.IsAccessModifierKeyword() )
                                ? TokenList()
                                : TokenList( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) ),
                            Token( isInit ? SyntaxKind.InitKeyword : SyntaxKind.SetKeyword ),
                            null,
                            null,
                            Token( SyntaxKind.SemicolonToken ) ) ) );
    }
}