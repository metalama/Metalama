// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Metalama.Framework.Engine.SyntaxGeneration.SyntaxFactoryEx;
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
            else
            {
                // Apply IInitializable call-site substitutions (WithInitialize(...)) to any
                // variable whose initializer contains object-creation / with expressions targeting
                // an IInitializable type. Each variable in a multi-variable declaration resolves to
                // its own field symbol and may have independent substitutions.
                fieldDeclaration = this.RewriteFieldInitializers( fieldDeclaration, context );
            }

            members.Add( fieldDeclaration );

            return members;
        }

        private FieldDeclarationSyntax RewriteFieldInitializers( FieldDeclarationSyntax fieldDeclaration, SyntaxGenerationContext context )
        {
            VariableDeclaratorSyntax[]? newVariables = null;

            for ( var i = 0; i < fieldDeclaration.Declaration.Variables.Count; i++ )
            {
                var variable = fieldDeclaration.Declaration.Variables[i];

                if ( variable.Initializer == null )
                {
                    continue;
                }

                var fieldSymbol = (IFieldSymbol?) this.IntermediateCompilationContext.SemanticModelProvider
                    .GetSemanticModel( variable.SyntaxTree )
                    .GetDeclaredSymbol( variable );

                if ( fieldSymbol == null )
                {
                    continue;
                }

                var rewrittenInitializer = this.RewriteInitializer( fieldSymbol, variable.Initializer, context );

                if ( ReferenceEquals( rewrittenInitializer, variable.Initializer ) )
                {
                    continue;
                }

                newVariables ??= fieldDeclaration.Declaration.Variables.ToArray();
                newVariables[i] = variable.WithInitializer( rewrittenInitializer );
            }

            if ( newVariables == null )
            {
                return fieldDeclaration;
            }

            return fieldDeclaration.WithDeclaration( fieldDeclaration.Declaration.WithVariables( SeparatedList( newVariables ) ) );
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
                                TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ),
                                TokenWithTrailingSpace( SyntaxKind.StaticKeyword ) )
                            : TokenList( TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) ),
                        type,
                        null,
                        WellKnownIdentifier( GetEmptyImplMemberName( symbol ) ),
                        accessorList.WithOptionalTrailingLineFeed( context ),
                        null,
                        null )
                    .WithOptionalLeadingLineFeed( context )
                    .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );
        }
    }
}