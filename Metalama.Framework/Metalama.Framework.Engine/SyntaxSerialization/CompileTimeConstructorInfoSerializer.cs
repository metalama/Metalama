// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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