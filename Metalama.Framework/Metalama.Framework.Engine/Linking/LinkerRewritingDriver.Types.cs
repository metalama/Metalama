// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerRewritingDriver
{
    public ClassDeclarationSyntax RewriteClass(
        ClassDeclarationSyntax classDeclaration,
        INamedTypeSymbol symbol,
        IReadOnlyList<MemberDeclarationSyntax> transformedMembers )
    {
        if ( this.LateTransformationRegistry.HasRemovedPrimaryConstructor( symbol ) )
        {
#if ROSLYN_4_8_0_OR_GREATER
            classDeclaration =
                classDeclaration.PartialUpdate(
                    RewritePrimaryConstructorTypeAttributeLists( classDeclaration.AttributeLists ),
                    parameterList: default(ParameterListSyntax),
                    baseList:
                    classDeclaration.BaseList?.WithTypes(
                        SeparatedList(
                            classDeclaration.BaseList.Types.SelectAsArray(
                                b =>
                                    b switch
                                    {
                                        PrimaryConstructorBaseTypeSyntax pc => SimpleBaseType( pc.Type ),
                                        _ => b
                                    } ) ) ) );
#else
            throw new AssertionFailedException( "This code should not run in this Roslyn version." );
#endif
        }

        classDeclaration = classDeclaration.WithMembers( List( transformedMembers ) );

        return classDeclaration;
    }

    public StructDeclarationSyntax RewriteStruct(
        StructDeclarationSyntax structDeclaration,
        INamedTypeSymbol symbol,
        IReadOnlyList<MemberDeclarationSyntax> transformedMembers )
    {
        if ( this.LateTransformationRegistry.HasRemovedPrimaryConstructor( symbol ) )
        {
#if ROSLYN_4_8_0_OR_GREATER
            structDeclaration =
                structDeclaration.PartialUpdate(
                    RewritePrimaryConstructorTypeAttributeLists( structDeclaration.AttributeLists ),
                    parameterList: default(ParameterListSyntax) );
#else
            throw new AssertionFailedException( "This code should not run in this Roslyn version." );
#endif
        }

        structDeclaration = structDeclaration.WithMembers( List( transformedMembers ) );

        return structDeclaration;
    }

    public RecordDeclarationSyntax RewriteRecord(
        RecordDeclarationSyntax recordDeclaration,
        INamedTypeSymbol symbol,
        IReadOnlyList<MemberDeclarationSyntax> transformedMembers )
    {
        if ( this.LateTransformationRegistry.HasRemovedPrimaryConstructor( symbol ) )
        {
            recordDeclaration =
                recordDeclaration.PartialUpdate(
                    RewritePrimaryConstructorTypeAttributeLists( recordDeclaration.AttributeLists ),
                    parameterList: default(ParameterListSyntax),
                    baseList:
                    recordDeclaration.BaseList?.WithTypes(
                        SeparatedList(
                            recordDeclaration.BaseList.Types.SelectAsArray(
                                b =>
                                    b switch
                                    {
                                        PrimaryConstructorBaseTypeSyntax pc => SimpleBaseType( pc.Type ),
                                        _ => b
                                    } ) ) ) );
        }
        else if ( recordDeclaration.ParameterList != null )
        {
            var semanticModel = this.IntermediateCompilationContext.SemanticModelProvider.GetSemanticModel( recordDeclaration.SyntaxTree );
            SyntaxGenerationContext? generationContext = null;

            List<MemberDeclarationSyntax>? newMembers = null;

            var transformedParametersAndCommas = new List<SyntaxNodeOrToken>( recordDeclaration.ParameterList.Parameters.Count * 2 );

            for ( var i = 0; i < recordDeclaration.ParameterList.Parameters.Count; i++ )
            {
                var parameter = recordDeclaration.ParameterList.Parameters[i];
                newMembers ??= new List<MemberDeclarationSyntax>();

                var parameterSymbol = semanticModel.GetDeclaredSymbol( parameter ).AssertNotNull();
                var propertySymbol = parameterSymbol.ContainingType.GetMembers( parameterSymbol.Name ).OfType<IPropertySymbol>().FirstOrDefault();

                if ( propertySymbol?.GetPrimaryDeclarationSyntax() is ParameterSyntax && this.IsRewriteTarget( propertySymbol ) )
                {
                    SyntaxGenerationContext GetSyntaxGenerationContext()
                    {
                        return generationContext ??= this.IntermediateCompilationContext.GetSyntaxGenerationContext(
                            this.SyntaxGenerationOptions,
                            recordDeclaration.SyntaxTree,
                            recordDeclaration.SpanStart );
                    }

                    // Add new members that take place of synthesized positional property.
                    newMembers.AddRange(
                        this.RewritePositionalProperty(
                            parameter,
                            propertySymbol.AssertNotNull(),
                            GetSyntaxGenerationContext() ) );

                    // Remove all attributes related to properties (property/field/get/set target specifiers).
                    var transformedParameter =
                        parameter.WithAttributeLists(
                            List(
                                parameter.AttributeLists
                                    .Where(
                                        l =>
                                            l.Target?.Identifier.IsKind( SyntaxKind.PropertyKeyword ) != true
                                            && l.Target?.Identifier.IsKind( SyntaxKind.FieldKeyword ) != true
                                            && l.Target?.Identifier.IsKind( SyntaxKind.GetKeyword ) != true
                                            && l.Target?.Identifier.IsKind( SyntaxKind.SetKeyword ) != true ) ) );

                    transformedParametersAndCommas.Add( transformedParameter );
                }
                else
                {
                    transformedParametersAndCommas.Add( parameter );
                }

                if ( i < recordDeclaration.ParameterList.Parameters.SeparatorCount )
                {
                    transformedParametersAndCommas.Add( recordDeclaration.ParameterList.Parameters.GetSeparator( i ) );
                }
            }

            recordDeclaration = recordDeclaration.WithParameterList(
                recordDeclaration.ParameterList.WithParameters( SeparatedList<ParameterSyntax>( transformedParametersAndCommas ) ) );

            if ( newMembers is { Count: > 0 } )
            {
                transformedMembers =
                    transformedMembers.Concat( newMembers );
            }
        }

        recordDeclaration = recordDeclaration.WithMembers( List( transformedMembers ) );

        return recordDeclaration;
    }

    private static SyntaxList<AttributeListSyntax> RewritePrimaryConstructorTypeAttributeLists( SyntaxList<AttributeListSyntax> attributeLists )
        => List( attributeLists.Where( al => al.Target?.Identifier.IsKind( SyntaxKind.MethodKeyword ) != true ) );
}