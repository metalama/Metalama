// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

internal sealed class TypeIdRefSerializer<T> : ReferenceTypeSerializer<TypeIdRef<T>>
    where T : class, IType
{
    public override TypeIdRef<T> CreateInstance( IArgumentsReader constructorArguments )
    {
        var id = constructorArguments.GetValue<string>( "id" ).AssertNotNull();

        return
            (TypeIdRef<T>)
            DurableRefFactory.FromTypeId<T>( new SerializableTypeId( id ) );
    }

    public override void SerializeObject( TypeIdRef<T> obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
    {
        constructorArguments.SetValue( "id", obj.Id );
    }

    public override void DeserializeFields( TypeIdRef<T> obj, IArgumentsReader initializationArguments ) { }
}