// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

internal sealed class SerializableDeclarationIdSerializer : ValueTypeSerializer<SerializableDeclarationId>
{
    public override void SerializeObject( SerializableDeclarationId obj, IArgumentsWriter constructorArguments )
        => constructorArguments.SetValue( "id", obj.Id );

    public override SerializableDeclarationId DeserializeObject( IArgumentsReader constructorArguments )
    {
        var id = constructorArguments.GetValue<string>( "id" ).AssertNotNull();

        return new SerializableDeclarationId( id );
    }
}