// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking.Substitution;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

// Properties with overrides have the following structure:
//  * Final semantic. 
//  * Override n
//  * ...
//  * Override 1
//  * Default semantic.
//  * Base semantic (if the property was introduced).

namespace Metalama.Framework.Engine.Linking
{
    internal sealed partial class LinkerRewritingDriver
    {
        private IReadOnlyList<MemberDeclarationSyntax> RewritePositionalProperty(
            ParameterSyntax recordParameter,
            IPropertySymbol symbol,
            SyntaxGenerationContext generationContext )
        {
            if ( this.InjectionRegistry.IsOverrideTarget( symbol ) )
            {
                var members = new List<MemberDeclarationSyntax>();
                var lastOverride = (IPropertySymbol) this.InjectionRegistry.GetLastOverride( symbol );

                if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
                {
                    // Backing field for auto property.
                    members.Add(
                        this.GetPropertyBackingField(
                            recordParameter.Type.AssertNotNull(),
                            EqualsValueClause( IdentifierName( recordParameter.Identifier.ValueText ) ),
                            FilterAttributeListsForTarget( recordParameter.AttributeLists, SyntaxKind.FieldKeyword, false, false ),
                            symbol,
                            generationContext ) );
                }

                if ( this.AnalysisRegistry.IsInlined( lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
                {
                    members.Add( GetLinkedDeclaration( IntermediateSymbolSemanticKind.Final ) );
                }
                else
                {
                    members.Add(
                        this.GetTrampolineForPositionalProperty(
                            recordParameter.Identifier,
                            recordParameter.Type.AssertNotNull(),
                            lastOverride,
                            generationContext ) );
                }

                if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                     && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                     && this.ShouldGenerateEmptyMember( symbol ) )
                {
                    members.Add(
                        this.GetEmptyImplProperty(
                            symbol,
                            List<AttributeListSyntax>(),
                            recordParameter.Type.AssertNotNull(),
                            generationContext ) );
                }

                return members;
            }
            else if ( this.InjectionRegistry.IsOverride( symbol ) )
            {
                if ( !this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                     || this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
                {
                    return Array.Empty<MemberDeclarationSyntax>();
                }

                return [GetLinkedDeclaration( IntermediateSymbolSemanticKind.Default )];
            }
            else
            {
                throw new AssertionFailedException( $"'{symbol}' is not an override target" );
            }

            MemberDeclarationSyntax GetLinkedDeclaration( IntermediateSymbolSemanticKind semanticKind )
            {
                var generatedAccessors = new List<AccessorDeclarationSyntax>
                {
                    GetLinkedAccessor(
                        semanticKind,
                        SyntaxKind.GetAccessorDeclaration,
                        symbol.GetMethod.AssertNotNull() ),
                    GetLinkedAccessor(
                        semanticKind,
                        symbol.SetMethod.AssertNotNull().IsInitOnly
                            ? SyntaxKind.InitAccessorDeclaration
                            : SyntaxKind.SetAccessorDeclaration,
                        symbol.SetMethod.AssertNotNull() )
                };

                return
                    PropertyDeclaration(
                        FilterAttributeListsForTarget( recordParameter.AttributeLists, SyntaxKind.PropertyKeyword, false, false ),
                        TokenList( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PublicKeyword ) ),
                        recordParameter.Type.AssertNotNull().WithOptionalTrailingTrivia( ElasticSpace, this.SyntaxGenerationOptions ),
                        null,
                        recordParameter.Identifier,
                        AccessorList( List( generatedAccessors ) ),
                        null,
                        null,
                        default );
            }

            AccessorDeclarationSyntax GetLinkedAccessor(
                IntermediateSymbolSemanticKind semanticKind,
                SyntaxKind accessorSyntaxKind,
                IMethodSymbol methodSymbol )
            {
                var linkedBody = this.GetSubstitutedBody(
                    methodSymbol.ToSemantic( semanticKind ),
                    new SubstitutionContext(
                        this,
                        generationContext,
                        new InliningContextIdentifier( methodSymbol.ToSemantic( semanticKind ) ) ) );

                var body =
                    linkedBody
                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock )
                        .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );

                return
                    AccessorDeclaration(
                            accessorSyntaxKind,
                            List<AttributeListSyntax>(),
                            TokenList(),
                            Token(
                                accessorSyntaxKind switch
                                {
                                    SyntaxKind.GetAccessorDeclaration => SyntaxKind.GetKeyword,
                                    SyntaxKind.SetAccessorDeclaration => SyntaxKind.SetKeyword,
                                    SyntaxKind.InitAccessorDeclaration => SyntaxKind.InitKeyword,
                                    _ => throw new AssertionFailedException( $"Unexpected syntax kind {accessorSyntaxKind}." )
                                } ),
                            null,
                            null,
                            default )
                        .WithBody( body );
            }
        }

        private PropertyDeclarationSyntax GetTrampolineForPositionalProperty(
            SyntaxToken identifier,
            TypeSyntax type,
            IPropertySymbol targetSymbol,
            SyntaxGenerationContext context )
        {
            var getAccessor =
                AccessorDeclaration(
                    SyntaxKind.GetAccessorDeclaration,
                    context.SyntaxGenerator.FormattedBlock(
                        ReturnStatement(
                            SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReturnKeyword ),
                            GetInvocationTarget(),
                            Token( SyntaxKind.SemicolonToken ) ) ) );

            var setAccessor =
                AccessorDeclaration(
                    targetSymbol.GetMethod.AssertNotNull().IsInitOnly ? SyntaxKind.InitAccessorDeclaration : SyntaxKind.SetAccessorDeclaration,
                    context.SyntaxGenerator.FormattedBlock(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                GetInvocationTarget(),
                                IdentifierName( "value" ) ) ) ) );

            return
                PropertyDeclaration(
                    List<AttributeListSyntax>(),
                    TokenList( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PublicKeyword ) ),
                    type.WithOptionalTrailingTrivia( ElasticSpace, this.SyntaxGenerationOptions ),
                    null,
                    identifier,
                    AccessorList( List( [getAccessor, setAccessor] ) ),
                    null,
                    null,
                    default );

            ExpressionSyntax GetInvocationTarget()
            {
                if ( targetSymbol.IsStatic )
                {
                    return IdentifierName( targetSymbol.Name );
                }
                else
                {
                    return MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName( targetSymbol.Name ) )
                        .WithSimplifierAnnotationIfNecessary( context );
                }
            }
        }
    }
}