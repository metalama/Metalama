// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

internal sealed class SerializableTypeIdSerializer : ValueTypeSerializer<SerializableTypeId>
{
    public override void SerializeObject( SerializableTypeId obj, IArgumentsWriter constructorArguments ) => constructorArguments.SetValue( "id", obj.Id );

    public override SerializableTypeId DeserializeObject( IArgumentsReader constructorArguments )
    {
        var id = constructorArguments.GetValue<string>( "id" ).AssertNotNull();

        return new SerializableTypeId( id );
    }
}