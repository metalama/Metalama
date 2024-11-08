// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Substitution;

internal sealed class EmptyPartialAccessorSubstitution(
    CompilationContext compilationContext,
    AccessorDeclarationSyntax rootNode,
    bool usingSimpleInlining,
    string? returnVariableIdentifier )
    : EmptyPartialMemberSubstitution( compilationContext, usingSimpleInlining, returnVariableIdentifier )
{
    public override SyntaxNode ReplacedNode => rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
        => currentNode switch
        {
            AccessorDeclarationSyntax => this.Substitute( substitutionContext ),
            _ => throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" ),
        };

    protected override bool IsVoid => false;
}