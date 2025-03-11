// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal sealed class CompileTimeConstructorInfoSerializer : MetalamaMethodBaseSerializer<CompileTimeConstructorInfo, ConstructorInfo>
{
    public override ExpressionSyntax Serialize( CompileTimeConstructorInfo obj, SyntaxSerializationContext serializationContext )
        => SyntaxFactory.ParenthesizedExpression(
                serializationContext.SyntaxGenerator.SafeCastExpression(
                    serializationContext.GetTypeSyntax( typeof(ConstructorInfo) ),
                    SerializeMethodBase( obj, serializationContext ) ) )
            .WithSimplifierAnnotationIfNecessary( serializationContext.SyntaxGenerationContext );

    public CompileTimeConstructorInfoSerializer( SyntaxSerializationService service ) : base( service ) { }
}