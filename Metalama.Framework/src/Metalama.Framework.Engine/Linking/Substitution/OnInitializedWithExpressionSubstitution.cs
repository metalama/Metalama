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
/// Substitutes a <c>with</c> expression to wrap with
/// <c>InitializableExtensions.WithInitialize(expr, InitializationMetadata.Modify)</c>.
/// </summary>
internal sealed class OnInitializedWithExpressionSubstitution : OnInitializedCallSiteSubstitution
{
    public OnInitializedWithExpressionSubstitution(
        CompilationContext compilationContext,
        SyntaxNode replacedNode,
        InitializableTypeInfo typeInfo )
        : base( compilationContext, replacedNode, typeInfo )
    {
    }

    public override SyntaxNode? Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        // Wrap in parentheses: (expr with { ... })
        var parenthesized = ParenthesizedExpression( (ExpressionSyntax) currentNode );

        // Wrap with InitializableExtensions.WithInitialize(expr, InitializationMetadata.Modify)
        return this.WrapWithInitializeCall( substitutionContext, parenthesized, CreateModifyMetadataExpression( substitutionContext ) );
    }

    /// <summary>
    /// Creates <c>global::Metalama.Framework.RunTime.Initialization.InitializationMetadata.Modify</c>.
    /// </summary>
    private static ExpressionSyntax CreateModifyMetadataExpression( SubstitutionContext substitutionContext )
    {
        return MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                substitutionContext.SyntaxGenerationContext.SyntaxGenerator.TypeSyntax( typeof(InitializationMetadata) ),
                SyntaxFactoryEx.SafeIdentifierName( nameof(InitializationMetadata.Modify) ) )
            .WithSimplifierAnnotation();
    }
}
