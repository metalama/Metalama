// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes accesses to event field delegate, i.e. the backing field.
/// </summary>
internal sealed class CallerMemberSubstitution : SyntaxNodeSubstitution
{
    private readonly SyntaxNode _rootNode;
    private readonly IMethodSymbol _referencingOverrideTarget;
    private readonly IMethodSymbol _targetMethod;
    private readonly IReadOnlyList<int> _parametersToFix;

    public CallerMemberSubstitution(
        CompilationContext compilationContext,
        SyntaxNode rootNode,
        IMethodSymbol referencingOverrideTarget,
        IMethodSymbol targetMethod,
        IReadOnlyList<int> parametersToFix )
        : base( compilationContext )
    {
        this._rootNode = rootNode;
        this._referencingOverrideTarget = referencingOverrideTarget;
        this._targetMethod = targetMethod;
        this._parametersToFix = parametersToFix;
    }

    public override SyntaxNode ReplacedNode => this._rootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        switch ( currentNode )
        {
            case InvocationExpressionSyntax invocationExpression:
                var additionalArguments = new List<ArgumentSyntax>();

                foreach ( var parameterToFix in this._parametersToFix )
                {
                    var parameter = this._targetMethod.Parameters[parameterToFix];

                    additionalArguments.Add(
                        Argument(
                            NameColon( parameter.Name ),
                            default,
                            LiteralExpression( SyntaxKind.StringLiteralExpression, Literal( this.GetTargetName() ) ) ) );
                }

                return
                    invocationExpression
                        .WithArgumentList(
                            invocationExpression.ArgumentList.WithArguments( invocationExpression.ArgumentList.Arguments.AddRange( additionalArguments ) ) );

            default:
                throw new AssertionFailedException( $"Unsupported syntax: {currentNode}" );
        }
    }

    private string GetTargetName()
    {
        switch ( this._referencingOverrideTarget )
        {
            case { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet }:
            case { MethodKind: MethodKind.EventAdd or MethodKind.EventRemove }:
                return this._referencingOverrideTarget.AssociatedSymbol.AssertNotNull().Name;

            default:
                return this._referencingOverrideTarget.Name;
        }
    }
}