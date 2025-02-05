// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using Accessibility = Metalama.Framework.Code.Accessibility;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Helpers;

internal static class BuilderHelpers
{
    public static void AddTokens( this Accessibility accessibility, List<SyntaxToken> tokenList )
    {
        switch ( accessibility )
        {
            case Accessibility.Private:
                tokenList.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) );

                break;

            case Accessibility.Protected:
                tokenList.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ProtectedKeyword ) );

                break;

            case Accessibility.Internal:
                tokenList.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.InternalKeyword ) );

                break;

            case Accessibility.PrivateProtected:
                tokenList.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) );
                tokenList.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ProtectedKeyword ) );

                break;

            case Accessibility.ProtectedInternal:
                tokenList.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ProtectedKeyword ) );
                tokenList.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.InternalKeyword ) );

                break;

            case Accessibility.Public:
                tokenList.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PublicKeyword ) );

                break;

            default:
                throw new AssertionFailedException( $"Unexpected Accessibility: {accessibility}." );
        }
    }

    /*
    public static void AddReturnValueTokens( this RefKind refKind, List<SyntaxToken> tokenList )
    {
        switch ( refKind )
        {
            case RefKind.Ref:
                tokenList.Add( Token( SyntaxKind.RefKeyword ).WithTrailingTrivia( Space ) );

                break;

            case RefKind.RefReadOnly:
                tokenList.Add( Token( SyntaxKind.RefKeyword ).WithTrailingTrivia( Space ) );
                tokenList.Add( Token( SyntaxKind.ReadOnlyKeyword ).WithTrailingTrivia( Space ) );

                break;

            case RefKind.Out:
                tokenList.Add( Token( SyntaxKind.OutKeyword ).WithTrailingTrivia( Space ) );

                break;

            case RefKind.None:
                break;

            default:
                throw new AssertionFailedException( $"Unexpected RefKind: {refKind}." );
        }
    }

    public static void AddParameterTokens( this RefKind refKind, List<SyntaxToken> tokenList )
    {
        switch ( refKind )
        {
            case RefKind.Ref:
                tokenList.Add( Token( SyntaxKind.RefKeyword ).WithTrailingTrivia( Space ) );

                break;

            case RefKind.In:
                tokenList.Add( Token( SyntaxKind.InKeyword ).WithTrailingTrivia( Space ) );

                break;

            case RefKind.Out:
                tokenList.Add( Token( SyntaxKind.OutKeyword ).WithTrailingTrivia( Space ) );

                break;

            case RefKind.None:
                break;

            default:
                throw new AssertionFailedException( $"{refKind} is not supported." );
        }
    }
    */
}