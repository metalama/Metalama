// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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