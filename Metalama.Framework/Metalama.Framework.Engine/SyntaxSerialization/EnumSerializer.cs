// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Globalization;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.SyntaxSerialization;

internal sealed class EnumSerializer : ObjectSerializer
{
    private static readonly Type[] _unsignedTypes = [typeof(ushort), typeof(uint), typeof(ulong), typeof(byte)];

    public override ExpressionSyntax Serialize( object obj, SyntaxSerializationContext serializationContext )
    {
        var o = (Enum) obj;

        var enumType = o.GetType();
        var typeName = serializationContext.GetTypeSyntax( enumType );
        var name = Enum.GetName( enumType, o );

        if ( name != null )
        {
            return MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                typeName,
                IdentifierName( name ) );
        }

        var underlyingType = Enum.GetUnderlyingType( o.GetType() );

        var literal = _unsignedTypes.Contains( underlyingType )
            ? Literal( Convert.ToUInt64( o, CultureInfo.InvariantCulture ) )
            : Literal( Convert.ToInt64( o, CultureInfo.InvariantCulture ) );

        return serializationContext.SyntaxGenerator.SafeCastExpression(
            typeName,
            ParenthesizedExpression(
                LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    literal ) ) );
    }

    public override Type InputType => typeof(Enum);

    public override Type OutputType => throw new NotSupportedException();

    public EnumSerializer( SyntaxSerializationService service ) : base( service ) { }
}