// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Inlining;

internal sealed class ImplicitLastOverrideReferenceInliner : Inliner
{
    public static ImplicitLastOverrideReferenceInliner Instance { get; } = new();

    private ImplicitLastOverrideReferenceInliner() { }

    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel ) => true;

    public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
    {
        SyntaxNode body =
            aspectReference.ContainingSemantic.Symbol.GetPrimaryDeclarationSyntax() switch
            {
                MethodDeclarationSyntax { Body: { } methodBody } => methodBody,
                MethodDeclarationSyntax { ExpressionBody: { } methodBody } => methodBody,
                MethodDeclarationSyntax { Body: null, ExpressionBody: null } partialMethodDeclaration => partialMethodDeclaration,
                DestructorDeclarationSyntax { Body: { } destructorBody } => destructorBody,
                DestructorDeclarationSyntax { ExpressionBody: { } destructorBody } => destructorBody,
                ConstructorDeclarationSyntax { Body: { } constructorBody } => constructorBody,
                ConstructorDeclarationSyntax { ExpressionBody: { } constructorBody } => constructorBody,
                OperatorDeclarationSyntax { Body: { } operatorBody } => operatorBody,
                OperatorDeclarationSyntax { ExpressionBody: { } operatorBody } => operatorBody,
                ConversionOperatorDeclarationSyntax { Body: { } conversionOperatorBody } => conversionOperatorBody,
                ConversionOperatorDeclarationSyntax { ExpressionBody: { } conversionOperatorBody } => conversionOperatorBody,
                AccessorDeclarationSyntax { Body: { } accessorBody } => accessorBody,
                AccessorDeclarationSyntax { ExpressionBody: { } accessorBody } => accessorBody,
                AccessorDeclarationSyntax { Body: null, ExpressionBody: null } accessor => accessor,
                ArrowExpressionClauseSyntax arrowExpressionClause => arrowExpressionClause,
                VariableDeclaratorSyntax { Parent.Parent: EventFieldDeclarationSyntax } eventFieldVariable => eventFieldVariable,
                ParameterSyntax { Parent: ParameterListSyntax { Parent: RecordDeclarationSyntax } } recordParameter => recordParameter,
#if ROSLYN_4_8_0_OR_GREATER
                TypeDeclarationSyntax { ParameterList: { } parameterList } => parameterList,
#endif
                _ => throw new AssertionFailedException( $"Declaration '{aspectReference.ContainingSemantic.Symbol}' has an unexpected declaration node." )
            };

        return new InliningAnalysisInfo( body, null );
    }

    public override bool IsValidForContainingSymbol( ISymbol symbol )
        => throw new AssertionFailedException( $"This method should not be called for '{symbol}'." );

    public override bool IsValidForTargetSymbol( ISymbol symbol ) => throw new AssertionFailedException( $"This method should not be called for '{symbol}'." );
}