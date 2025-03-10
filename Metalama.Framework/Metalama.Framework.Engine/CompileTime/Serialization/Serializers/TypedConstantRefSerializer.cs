// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

internal sealed class TypedConstantRefSerializer : ValueTypeSerializer<TypedConstantRef>
{
    public override void SerializeObject( TypedConstantRef obj, IArgumentsWriter constructorArguments )
    {
        constructorArguments.SetValue( "value", obj.RawValue );
        constructorArguments.SetValue( "type", obj.Type );
    }

    public override TypedConstantRef DeserializeObject( IArgumentsReader constructorArguments )
    {
        var value = constructorArguments.GetValue<object?>( "value" );
        var type = constructorArguments.GetValue<IRef<IType>>( "type" );

        return new TypedConstantRef( value, type );
    }
}