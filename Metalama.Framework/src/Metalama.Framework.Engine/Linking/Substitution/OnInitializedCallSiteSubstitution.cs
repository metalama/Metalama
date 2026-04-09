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
/// Base class for substitutions that wrap object creation or <c>with</c> expressions with
/// <c>InitializableExtensions.WithInitialize(expr)</c> calls.
/// </summary>
internal abstract class OnInitializedCallSiteSubstitution : SyntaxNodeSubstitution
{
    private readonly SyntaxNode _replacedNode;
    protected InitializableTypeInfo TypeInfo { get; }

    protected OnInitializedCallSiteSubstitution(
        CompilationContext compilationContext,
        SyntaxNode replacedNode,
        InitializableTypeInfo typeInfo )
        : base( compilationContext )
    {
        this._replacedNode = replacedNode;
        this.TypeInfo = typeInfo;
    }

    public override SyntaxNode ReplacedNode => this._replacedNode;

    /// <summary>
    /// Wraps an expression with <c>InitializableExtensions.WithInitialize(expr)</c> or
    /// <c>InitializableExtensions.WithInitialize(expr, metadata)</c>.
    /// </summary>
    protected static ExpressionSyntax WrapWithInitializeCall(
        SubstitutionContext substitutionContext,
        ExpressionSyntax expression,
        ExpressionSyntax? metadataArgument = null )
    {
        var initializableExtensionsType =
            substitutionContext.SyntaxGenerationContext.SyntaxGenerator.TypeSyntax( typeof(InitializableExtensions) );

        var initializedAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            initializableExtensionsType,
            SyntaxFactoryEx.SafeIdentifierName( nameof(InitializableExtensions.WithInitialize) ) );

        ArgumentListSyntax argList;

        if ( metadataArgument != null )
        {
            argList = ArgumentList(
                SeparatedList(
                    new[]
                    {
                        Argument( expression ),
                        Argument( metadataArgument )
                    } ) );
        }
        else
        {
            argList = ArgumentList( SingletonSeparatedList( Argument( expression ) ) );
        }

        return InvocationExpression( initializedAccess, argList ).WithSimplifierAnnotation();
    }
}
