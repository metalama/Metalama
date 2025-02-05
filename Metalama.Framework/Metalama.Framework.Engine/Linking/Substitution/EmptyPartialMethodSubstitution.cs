// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Substitution;

internal sealed class EmptyPartialMethodSubstitution : EmptyPartialMemberSubstitution
{
    private readonly MethodDeclarationSyntax _rootNode;

    public EmptyPartialMethodSubstitution( CompilationContext compilationContext, MethodDeclarationSyntax rootNode, bool usingSimpleInlining, string? returnVariableIdentifier )
        : base( compilationContext, usingSimpleInlining, returnVariableIdentifier )
    {
        this._rootNode = rootNode;
    }

    public override SyntaxNode ReplacedNode => this._rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
        => currentNode switch
        {
            MethodDeclarationSyntax => this.Substitute( substitutionContext ),
            _ => throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" ),
        };

    protected override bool IsVoid => this._rootNode.ReturnType is PredefinedTypeSyntax predefinedType && predefinedType.Keyword.IsKind( SyntaxKind.VoidKeyword );
}