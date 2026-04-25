// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Metalama.Framework.Engine.SyntaxGeneration.SyntaxFactoryEx;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Shared helper for generating <c>Deconstruct</c> method overloads for records.
/// Used by both <see cref="LinkerInjectionStep"/> and <see cref="LinkerRewritingDriver"/>.
/// </summary>
internal static class RecordDeconstructSyntaxHelper
{
    /// <summary>
    /// Generates a <c>Deconstruct</c> method with <c>out</c> parameters corresponding to the given
    /// record primary constructor parameter list.
    /// </summary>
    internal static MethodDeclarationSyntax GenerateDeconstructMethod(
        ParameterListSyntax parameterList,
        SyntaxGenerationContext context )
    {
        var parameters = (IReadOnlyList<ParameterSyntax>) parameterList.Parameters;

        var outParameters = parameters.SelectAsArray(
            p =>
                Parameter(
                    List<AttributeListSyntax>(),
                    TokenList( TokenWithTrailingSpace( SyntaxKind.OutKeyword ) ),
                    p.Type,
                    p.Identifier,
                    null ) );

        var assignments = parameters.SelectAsArray(
            p =>
                (StatementSyntax) ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        WellKnownIdentifierName( p.Identifier ),
                        MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                ThisExpression(),
                                WellKnownIdentifierName( p.Identifier ) )
                            .WithSimplifierAnnotationIfNecessary( context ) ) ) );

        return MethodDeclaration(
                List<AttributeListSyntax>(),
                TokenList( TokenWithTrailingSpace( SyntaxKind.PublicKeyword ) ),
                PredefinedType( TokenWithTrailingSpace( SyntaxKind.VoidKeyword ) ),
                null,
                Identifier( "Deconstruct" ),
                null,
                ParameterList( SeparatedList( outParameters ) ),
                List<TypeParameterConstraintClauseSyntax>(),
                Block( assignments ),
                null,
                default )
            .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );
    }
}