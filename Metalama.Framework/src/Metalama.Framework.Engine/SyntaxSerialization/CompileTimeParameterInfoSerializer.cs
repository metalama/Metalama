// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal sealed class CompileTimeParameterInfoSerializer : ObjectSerializer<CompileTimeParameterInfo, ParameterInfo>
{
    public override ExpressionSyntax Serialize( CompileTimeParameterInfo obj, SyntaxSerializationContext serializationContext )
        => SerializeParameter( obj.Target.GetTarget( serializationContext.CompilationModel ).AssertNotNull(), serializationContext );

    public static ExpressionSyntax SerializeParameter( IParameter parameter, SyntaxSerializationContext serializationContext )
    {
        var declaringMember = parameter.DeclaringMember;

        ExpressionSyntax memberExpression;
        string getParametersMethodName;

        switch ( declaringMember )
        {
            case IMethodBase method:
                memberExpression = CompileTimeMethodInfoSerializer.SerializeMethodBase( method, serializationContext );
                getParametersMethodName = nameof(MethodBase.GetParameters);

                break;

            case IIndexer indexer:
                memberExpression = CompileTimePropertyInfoSerializer.SerializeProperty( indexer, serializationContext );
                getParametersMethodName = nameof(PropertyInfo.GetIndexParameters);

                break;

            default:
                throw new AssertionFailedException( $"Unexpected declaration type for '{declaringMember}'." );
        }

        return ElementAccessExpression(
                InvocationExpression(
                    MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, memberExpression, IdentifierName( getParametersMethodName ) ) ),
                BracketedArgumentList(
                    SingletonSeparatedList( Argument( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( parameter.Index ) ) ) ) ) )
            .NormalizeWhitespaceIfNecessary( serializationContext.SyntaxGenerationContext );
    }

    public CompileTimeParameterInfoSerializer( SyntaxSerializationService service ) : base( service ) { }
}