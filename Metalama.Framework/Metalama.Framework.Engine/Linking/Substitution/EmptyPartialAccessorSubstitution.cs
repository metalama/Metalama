// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Substitution;

internal sealed class EmptyPartialAccessorSubstitution : EmptyPartialMemberSubstitution
{
    private readonly AccessorDeclarationSyntax _rootNode;

    public EmptyPartialAccessorSubstitution( CompilationContext compilationContext, AccessorDeclarationSyntax rootNode, bool usingSimpleInlining, string? returnVariableIdentifier )
        : base( compilationContext, usingSimpleInlining, returnVariableIdentifier )
    {
        this._rootNode = rootNode;
    }

    public override SyntaxNode ReplacedNode => this._rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
        => currentNode switch
        {
            AccessorDeclarationSyntax => this.Substitute( substitutionContext ),
            _ => throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" ),
        };

    protected override bool IsVoid => false;
}