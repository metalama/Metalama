// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal sealed class CompileTimeMethodInfoSerializer : MetalamaMethodBaseSerializer<CompileTimeMethodInfo, MethodInfo>
{
    public CompileTimeMethodInfoSerializer( SyntaxSerializationService service ) : base( service ) { }

    public override ExpressionSyntax Serialize( CompileTimeMethodInfo obj, SyntaxSerializationContext serializationContext )
        => ParenthesizedExpression(
                serializationContext.SyntaxGenerator.SafeCastExpression(
                    serializationContext.GetTypeSyntax( typeof(MethodInfo) ),
                    SerializeMethodBase( obj, serializationContext ) ) )
            .WithSimplifierAnnotationIfNecessary( serializationContext.SyntaxGenerationContext );
}