// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

/// <summary>
/// Serializes a <see cref="DurableExpression"/> as its text, a reference to its type, and its flags.
/// </summary>
/// <remarks>
/// The type is persisted as a reference. An expression used to be persisted as text alone and rebuilt by parsing it,
/// which typed it as <see cref="object"/>, because that is what <c>ParseExpression</c> substitutes when given no
/// type.
/// </remarks>
internal sealed class DurableExpressionSerializer : ReferenceTypeSerializer<DurableExpression>
{
    public override DurableExpression CreateInstance( IArgumentsReader constructorArguments )
        => DurableExpression.FromText(
            constructorArguments.GetValue<string>( "text" ).AssertNotNull(),
            constructorArguments.GetValue<IDurableRef<IType>>( "type" ).AssertNotNull(),
            constructorArguments.GetValue<bool?>( "isReferenceable" ),
            constructorArguments.GetValue<bool>( "isAssignable" ) );

    public override void SerializeObject( DurableExpression obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
    {
        constructorArguments.SetValue( "text", obj.Text );
        constructorArguments.SetValue( "type", obj.Type );
        constructorArguments.SetValue( "isReferenceable", obj.IsReferenceable );
        constructorArguments.SetValue( "isAssignable", obj.IsAssignable );
    }
}
