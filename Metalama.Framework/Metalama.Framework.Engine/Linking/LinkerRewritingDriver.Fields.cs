// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking
{
    internal sealed partial class LinkerRewritingDriver
    {
        private IReadOnlyList<MemberDeclarationSyntax> RewriteField(
            FieldDeclarationSyntax fieldDeclaration,
            IFieldSymbol symbol,
            SyntaxGenerationContext context )
        {
            Invariant.Assert( !this.InjectionRegistry.IsOverrideTarget( symbol ) );

            var members = new List<MemberDeclarationSyntax>();

            if ( this.AnalysisRegistry.IsReachable( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                 && !this.AnalysisRegistry.IsInlined( symbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) )
                 && this.ShouldGenerateEmptyMember( symbol ) )
            {
                members.Add(
                    GetEmptyImplField(
                        symbol,
                        List<AttributeListSyntax>(),
                        fieldDeclaration.Declaration.Type,
                        context ) );
            }

            if ( this.LateTransformationRegistry.IsPrimaryConstructorInitializedMember( symbol ) )
            {
                fieldDeclaration =
                    fieldDeclaration.WithDeclaration(
                        fieldDeclaration.Declaration.WithVariables(
                            SeparatedList( fieldDeclaration.Declaration.Variables.SelectAsArray( v => v.WithInitializer( default ) ) ) ) );
            }

            members.Add( fieldDeclaration );

            return members;
        }

        private static MemberDeclarationSyntax GetEmptyImplField(
            IFieldSymbol symbol,
            SyntaxList<AttributeListSyntax> attributes,
            TypeSyntax type,
            SyntaxGenerationContext context )
        {
            var setAccessorKind =
                symbol switch
                {
                    { IsReadOnly: false } => SyntaxKind.SetAccessorDeclaration,
                    { IsReadOnly: true } => SyntaxKind.InitAccessorDeclaration
                };

            var accessorList =
                AccessorList(
                    List(
                    [
                        AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration,
                            List<AttributeListSyntax>(),
                            TokenList(),
                            Token( SyntaxKind.GetKeyword ),
                            null,
                            ArrowExpressionClause( DefaultExpression( type ) ),
                            Token( SyntaxKind.SemicolonToken ) ),
                        AccessorDeclaration(
                            setAccessorKind,
                            context.SyntaxGenerator.FormattedBlock() )
                    ] ) );

            return
                PropertyDeclaration(
                        attributes,
                        symbol.IsStatic
                            ? TokenList(
                                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ),
                                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.StaticKeyword ) )
                            : TokenList( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) ),
                        type,
                        null,
                        Identifier( GetEmptyImplMemberName( symbol ) ),
                        accessorList.WithOptionalTrailingLineFeed( context ),
                        null,
                        null )
                    .WithOptionalLeadingLineFeed( context )
                    .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );
        }
    }
}