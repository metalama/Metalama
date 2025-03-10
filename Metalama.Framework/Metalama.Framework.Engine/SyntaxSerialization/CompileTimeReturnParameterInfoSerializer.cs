// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.ReflectionMocks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal sealed class CompileTimeReturnParameterInfoSerializer : ObjectSerializer<CompileTimeReturnParameterInfo, ParameterInfo>
    {
        public override ExpressionSyntax Serialize( CompileTimeReturnParameterInfo obj, SyntaxSerializationContext serializationContext )
            => SerializeParameter( obj.Target.GetTarget( serializationContext.CompilationModel ).AssertNotNull(), serializationContext );

        public static ExpressionSyntax SerializeParameter( IParameter parameter, SyntaxSerializationContext serializationContext )
        {
            var memberExpression = parameter.DeclaringMember switch
            {
                IMethod method => CompileTimeMethodInfoSerializer.SerializeMethodBase( method, serializationContext ),
                IIndexer indexer => CompileTimePropertyInfoSerializer.SerializeProperty( indexer, serializationContext ),
                _ => throw new AssertionFailedException( $"Unexpected declaration type for '{parameter.DeclaringMember}'." )
            };

            return MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, memberExpression, IdentifierName( "ReturnParameter" ) );
        }

        public CompileTimeReturnParameterInfoSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}