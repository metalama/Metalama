// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking.Inlining;

internal sealed class ImplicitLastOverrideReferenceInliner : Inliner
{
    public static ImplicitLastOverrideReferenceInliner Instance { get; } = new();

    private ImplicitLastOverrideReferenceInliner() { }

    public override bool CanInline( ResolvedAspectReference aspectReference, SemanticModel semanticModel ) => true;

    public override InliningAnalysisInfo GetInliningAnalysisInfo( ResolvedAspectReference aspectReference )
    {
        var declarationSyntax = aspectReference.ContainingSemantic.Symbol.GetPrimaryDeclarationSyntax();

        SyntaxNode body =
            declarationSyntax?.Kind() switch
            {
                SyntaxKind.MethodDeclaration when declarationSyntax is MethodDeclarationSyntax { Body: { } methodBody } => methodBody,
                SyntaxKind.MethodDeclaration when declarationSyntax is MethodDeclarationSyntax { ExpressionBody: { } methodBody } => methodBody,
                SyntaxKind.MethodDeclaration when declarationSyntax is MethodDeclarationSyntax
                {
                    Body: null, ExpressionBody: null
                } partialMethodDeclaration => partialMethodDeclaration,
                SyntaxKind.DestructorDeclaration when declarationSyntax is DestructorDeclarationSyntax { Body: { } destructorBody } => destructorBody,
                SyntaxKind.DestructorDeclaration when declarationSyntax is DestructorDeclarationSyntax { ExpressionBody: { } destructorBody } => destructorBody,
                SyntaxKind.ConstructorDeclaration when declarationSyntax is ConstructorDeclarationSyntax { Body: { } constructorBody } => constructorBody,
                SyntaxKind.ConstructorDeclaration when declarationSyntax is ConstructorDeclarationSyntax { ExpressionBody: { } constructorBody } =>
                    constructorBody,
                SyntaxKind.ConstructorDeclaration when declarationSyntax is ConstructorDeclarationSyntax
                {
                    Body: null, ExpressionBody: null
                } constructor => constructor,
                SyntaxKind.OperatorDeclaration when declarationSyntax is OperatorDeclarationSyntax { Body: { } operatorBody } => operatorBody,
                SyntaxKind.OperatorDeclaration when declarationSyntax is OperatorDeclarationSyntax { ExpressionBody: { } operatorBody } => operatorBody,
                SyntaxKind.ConversionOperatorDeclaration when declarationSyntax is ConversionOperatorDeclarationSyntax { Body: { } conversionOperatorBody } =>
                    conversionOperatorBody,
                SyntaxKind.ConversionOperatorDeclaration when declarationSyntax is ConversionOperatorDeclarationSyntax
                {
                    ExpressionBody: { } conversionOperatorBody
                } => conversionOperatorBody,
                { IsAccessorDeclaration: true } when declarationSyntax is AccessorDeclarationSyntax { Body: { } accessorBody } => accessorBody,
                { IsAccessorDeclaration: true } when declarationSyntax is AccessorDeclarationSyntax { ExpressionBody: { } accessorBody } => accessorBody,
                { IsAccessorDeclaration: true } when declarationSyntax is AccessorDeclarationSyntax { Body: null, ExpressionBody: null } accessor => accessor,
                SyntaxKind.ArrowExpressionClause when declarationSyntax is ArrowExpressionClauseSyntax arrowExpressionClause => arrowExpressionClause,
                SyntaxKind.VariableDeclarator when declarationSyntax is VariableDeclaratorSyntax
                {
                    Parent.Parent: EventFieldDeclarationSyntax
                } eventFieldVariable => eventFieldVariable,
                SyntaxKind.Parameter when declarationSyntax is ParameterSyntax
                {
                    Parent: ParameterListSyntax { Parent: RecordDeclarationSyntax }
                } recordParameter => recordParameter,
#if ROSLYN_4_8_0_OR_GREATER
                { IsTypeDeclaration: true } when declarationSyntax is TypeDeclarationSyntax { ParameterList: { } parameterList } => parameterList,
#endif
                _ => throw new AssertionFailedException( $"Declaration '{aspectReference.ContainingSemantic.Symbol}' has an unexpected declaration node." )
            };

        return new InliningAnalysisInfo( body, null );
    }

    public override bool IsValidForContainingSymbol( ISymbol symbol )
        => throw new AssertionFailedException( $"This method should not be called for '{symbol}'." );

    public override bool IsValidForTargetSymbol( ISymbol symbol ) => throw new AssertionFailedException( $"This method should not be called for '{symbol}'." );
}