// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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