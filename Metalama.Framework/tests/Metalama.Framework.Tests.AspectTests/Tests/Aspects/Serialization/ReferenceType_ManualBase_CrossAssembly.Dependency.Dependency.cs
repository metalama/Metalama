using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.ReferenceType_ManualBase_CrossAssembly;

[RunTimeOrCompileTime]
public class BaseSerializableType_ParameterlessCtor : ICompileTimeSerializable
{
    public int Foo { get; set; }

    public class Serializer_Custom : ReferenceTypeSerializer
    {
        public override object CreateInstance( Type type, IArgumentsReader constructorArguments ) => new BaseSerializableType_ParameterlessCtor();

        public override void DeserializeFields( object obj, IArgumentsReader initializationArguments )
            => ( (BaseSerializableType_ParameterlessCtor)obj ).Foo = initializationArguments.GetValue<int>( "Foo" );

        public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
            => initializationArguments.SetValue( "Foo", ( (BaseSerializableType_ParameterlessCtor)obj ).Foo );
    }
}

[RunTimeOrCompileTime]
public class BaseSerializableType_DeserializingCtor : ICompileTimeSerializable
{
    public int Foo { get; set; }

    public BaseSerializableType_DeserializingCtor() { }

    public BaseSerializableType_DeserializingCtor( IArgumentsReader reader )
    {
        Foo = reader.GetValue<int>( "Foo" );
    }

    public class Serializer_Custom : ReferenceTypeSerializer
    {
        public override object CreateInstance( Type type, IArgumentsReader constructorArguments )
            => new BaseSerializableType_DeserializingCtor( constructorArguments );

        public override void DeserializeFields( object obj, IArgumentsReader initializationArguments ) { }

        public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
            => constructorArguments.SetValue( "Foo", ( (BaseSerializableType_DeserializingCtor)obj ).Foo );
    }
}