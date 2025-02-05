// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Reflection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal sealed class CompileTimeEventInfoSerializer : ObjectSerializer<CompileTimeEventInfo, EventInfo>
{
    public override ExpressionSyntax Serialize( CompileTimeEventInfo obj, SyntaxSerializationContext serializationContext )
        => SerializeEvent( obj.Target.GetTarget( serializationContext.CompilationModel ).AssertNotNull(), serializationContext );

    public static ExpressionSyntax SerializeEvent( IEvent @event, SyntaxSerializationContext serializationContext )
    {
        var eventName = @event.Name;

        var typeCreation =
            TypeSerializationHelper.SerializeTypeSymbolRecursive(
                @event.DeclaringType.GetSymbol().AssertSymbolNullNotImplemented( UnsupportedFeatures.IntroducedTypeReflectionWrappers ),
                serializationContext );

        ExpressionSyntax result = InvocationExpression(
                MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, typeCreation, IdentifierName( "GetEvent" ) ),
                ArgumentList(
                    SeparatedList(
                    [
                        Argument( LiteralExpression( SyntaxKind.StringLiteralExpression, Literal( eventName ) ) ),
                        Argument( SyntaxUtility.CreateBindingFlags( @event, serializationContext ) )
                    ] ) ) )
            .NormalizeWhitespaceIfNecessary( serializationContext.SyntaxGenerationContext );

        // In the new .NET, the API is marked for nullability, so we have to suppress the warning.
        result = PostfixUnaryExpression( SyntaxKind.SuppressNullableWarningExpression, result );

        return result;
    }

    public CompileTimeEventInfoSerializer( SyntaxSerializationService service ) : base( service ) { }

    protected override ImmutableArray<Type> AdditionalSupportedTypes => ImmutableArray.Create( typeof(MemberInfo) );
}