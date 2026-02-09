// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitution for the original body of a compiler-synthesized record member (e.g. Equals, GetHashCode).
/// Generates a throw NotSupportedException since the original implementation is compiler-generated and cannot be called.
/// </summary>
internal sealed class SynthesizedRecordMemberSubstitution : SyntaxNodeSubstitution
{
    private readonly RecordDeclarationSyntax _rootNode;

    public SynthesizedRecordMemberSubstitution(
        CompilationContext compilationContext,
        RecordDeclarationSyntax rootNode )
        : base( compilationContext )
    {
        this._rootNode = rootNode;
    }

    public override SyntaxNode ReplacedNode => this._rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        var syntaxGenerator = substitutionContext.SyntaxGenerationContext.SyntaxGenerator;

        var throwStatement =
            ThrowStatement(
                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ThrowKeyword ),
                ObjectCreationExpression(
                    SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.NewKeyword ),
                    QualifiedName(
                        AliasQualifiedName(
                            SyntaxFactoryEx.WellKnownIdentifierName( Token( SyntaxKind.GlobalKeyword ) ),
                            SyntaxFactoryEx.WellKnownIdentifierName( "System" ) ),
                        SyntaxFactoryEx.WellKnownIdentifierName( "NotSupportedException" ) ),
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal(
                                        "Calling the original implementation of a compiler-synthesized record member is not supported. Do not use meta.Proceed() when overriding synthesized record members like Equals or GetHashCode." ) ) ) ) ),
                    null ),
                Token( SyntaxKind.SemicolonToken ) );

        return syntaxGenerator.FormattedBlock( throwStatement )
            .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
    }
}
