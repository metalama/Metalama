// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal sealed class CompileTimePropertyInfoSerializer : ObjectSerializer<CompileTimePropertyInfo, PropertyInfo>
{
    public CompileTimePropertyInfoSerializer( SyntaxSerializationService service ) : base( service ) { }

    public override ExpressionSyntax Serialize( CompileTimePropertyInfo obj, SyntaxSerializationContext serializationContext )
    {
        var property = obj.Target.GetTarget( serializationContext.CompilationModel ).AssertNotNull();

        return SerializeProperty( property, serializationContext );
    }

    public static ExpressionSyntax SerializeProperty( IPropertyOrIndexer propertyOrIndexer, SyntaxSerializationContext serializationContext )
    {
        var typeCreation = TypeSerializationHelper.SerializeTypeSymbolRecursive(
            propertyOrIndexer.DeclaringType.GetSymbol().AssertSymbolNullNotImplemented( UnsupportedFeatures.IntroducedTypeReflectionWrappers ),
            serializationContext );

        ExpressionSyntax result;

        switch ( propertyOrIndexer )
        {
            case IProperty:
                result = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        typeCreation,
                        IdentifierName( "GetProperty" ) ),
                    ArgumentList(
                        SeparatedList(
                        [
                            Argument(
                                LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal( propertyOrIndexer.Name ) ) ),
                            Argument( SyntaxUtility.CreateBindingFlags( propertyOrIndexer, serializationContext ) )
                        ] ) ) );

                break;

            case IIndexer indexer:
                {
                    var returnTypeCreation = TypeSerializationHelper.SerializeTypeSymbolRecursive(
                        propertyOrIndexer.Type.GetSymbol().AssertSymbolNullNotImplemented( UnsupportedFeatures.IntroducedTypeReflectionWrappers ),
                        serializationContext );

                    var parameterTypes = new List<ExpressionSyntax>();

                    foreach ( var parameter in indexer.Parameters )
                    {
                        var parameterType = TypeSerializationHelper.SerializeTypeSymbolRecursive(
                            parameter.Type.GetSymbol().AssertSymbolNullNotImplemented( UnsupportedFeatures.IntroducedTypeReflectionWrappers ),
                            serializationContext );

                        parameterTypes.Add( parameterType );
                    }

                    result = InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            typeCreation,
                            IdentifierName( "GetProperty" ) ),
                        ArgumentList(
                            SeparatedList(
                            [
                                Argument(
                                    LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        Literal(
                                            indexer.GetSymbol()
                                                .AssertSymbolNullNotImplemented( UnsupportedFeatures.IntroducedTypeReflectionWrappers )
                                                .MetadataName ) ) ),
                                Argument( SyntaxUtility.CreateBindingFlags( propertyOrIndexer, serializationContext ) ),
                                Argument( SyntaxFactoryEx.Null ), // binder
                                Argument( returnTypeCreation ),
                                Argument(
                                    ArrayCreationExpression(
                                            ArrayType( serializationContext.GetTypeSyntax( typeof(Type) ) )
                                                .WithRankSpecifiers(
                                                    SingletonList(
                                                        ArrayRankSpecifier( SingletonSeparatedList<ExpressionSyntax>( OmittedArraySizeExpression() ) ) ) ) )
                                        .WithInitializer(
                                            InitializerExpression(
                                                SyntaxKind.ArrayInitializerExpression,
                                                SeparatedList( parameterTypes ) ) ) ),
                                Argument( SyntaxFactoryEx.Null )
                            ] ) ) ); // modifiers

                    break;
                }

            default:
                throw new AssertionFailedException( $"Unexpected type: {propertyOrIndexer.DeclarationKind}." );
        }

        // In the new .NET, the API is marked for nullability, so we have to suppress the warning.
        result = PostfixUnaryExpression( SyntaxKind.SuppressNullableWarningExpression, result );

        return result;
    }

    protected override ImmutableArray<Type> AdditionalSupportedTypes => ImmutableArray.Create( typeof(MemberInfo) );
}