// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal sealed class CompileTimeFieldInfoSerializer : ObjectSerializer<CompileTimeFieldInfo, FieldInfo>
{
    public CompileTimeFieldInfoSerializer( SyntaxSerializationService service ) : base( service ) { }

    public override ExpressionSyntax Serialize( CompileTimeFieldInfo obj, SyntaxSerializationContext serializationContext )
    {
        var field = (IField) obj.Target.GetTarget( serializationContext.CompilationModel ).AssertNotNull();

        return SerializeField( field, serializationContext );
    }

    public static ExpressionSyntax SerializeField( IField field, SyntaxSerializationContext serializationContext )
    {
        var typeCreation =
            TypeSerializationHelper.SerializeTypeSymbolRecursive(
                field.DeclaringType.GetSymbol().AssertSymbolNullNotImplemented( UnsupportedFeatures.IntroducedTypeReflectionWrappers ),
                serializationContext );

        var allBindingFlags = SyntaxUtility.CreateBindingFlags( field, serializationContext );

        ExpressionSyntax fieldInfo = InvocationExpression(
                MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, typeCreation, IdentifierName( "GetField" ) ),
                ArgumentList(
                    SeparatedList( [Argument( LiteralExpression( SyntaxKind.StringLiteralExpression, Literal( field.Name ) ) ), Argument( allBindingFlags )] ) ) )
            .NormalizeWhitespaceIfNecessary( serializationContext.SyntaxGenerationContext );

        // In the new .NET, the API is marked for nullability, so we have to suppress the warning.
        fieldInfo = PostfixUnaryExpression( SyntaxKind.SuppressNullableWarningExpression, fieldInfo );

        return fieldInfo;
    }
}