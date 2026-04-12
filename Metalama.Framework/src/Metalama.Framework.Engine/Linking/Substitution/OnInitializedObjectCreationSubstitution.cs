// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.RunTime.Initialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes an object creation expression to wrap with
/// <c>InitializableExtensions.WithInitialize(expr)</c> and optionally
/// inject an <c>InitializationContext.WillInitialize</c> named constructor argument.
/// </summary>
internal sealed class OnInitializedObjectCreationSubstitution : OnInitializedCallSiteSubstitution
{
    private readonly string? _contextParamName;

    public OnInitializedObjectCreationSubstitution(
        CompilationContext compilationContext,
        SyntaxNode replacedNode,
        InitializableTypeInfo typeInfo,
        string? contextParamName )
        : base( compilationContext, replacedNode, typeInfo )
    {
        this._contextParamName = contextParamName;
    }

    public override SyntaxNode? Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        var expression = (ExpressionSyntax) currentNode;

        // Append named InitializationContext.WillInitialize argument if the constructor accepts it.
        if ( this._contextParamName != null )
        {
            expression = this.AppendWillInitializeArgument( expression, substitutionContext );
        }

        // Wrap with InitializableExtensions.WithInitialize(expr)
        return this.WrapWithInitializeCall( substitutionContext, expression );
    }

    private ExpressionSyntax AppendWillInitializeArgument( ExpressionSyntax expression, SubstitutionContext substitutionContext )
    {
        var willInitializeArg = Argument(
            nameColon: NameColon( SyntaxFactoryEx.SafeIdentifierName( this._contextParamName! ) ),
            refKindKeyword: default,
            expression: CreateWillInitializeExpression( substitutionContext ) );

        switch ( expression.Kind() )
        {
            case SyntaxKind.ObjectCreationExpression:
                {
                    var objectCreation = (ObjectCreationExpressionSyntax) expression;
                    var argList = objectCreation.ArgumentList ?? ArgumentList();
                    var newArgList = argList.AddArguments( willInitializeArg );

                    return objectCreation.WithArgumentList( newArgList );
                }

            case SyntaxKind.ImplicitObjectCreationExpression:
                {
                    var implicitCreation = (ImplicitObjectCreationExpressionSyntax) expression;
                    var newArgList = implicitCreation.ArgumentList.AddArguments( willInitializeArg );

                    return implicitCreation.WithArgumentList( newArgList );
                }

            default:
                return expression;
        }
    }

    /// <summary>
    /// Creates <c>global::Metalama.Framework.RunTime.Initialization.InitializationContext.WillInitialize</c>.
    /// </summary>
    private static ExpressionSyntax CreateWillInitializeExpression( SubstitutionContext substitutionContext )
    {
        return MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                substitutionContext.SyntaxGenerationContext.SyntaxGenerator.TypeSyntax( typeof(InitializationContext) ),
                SyntaxFactoryEx.SafeIdentifierName( nameof(InitializationContext.WillInitialize) ) )
            .WithSimplifierAnnotation();
    }
}
