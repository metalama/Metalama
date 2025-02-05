using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.ReferenceType_ManualBase;

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

public class SerializableType_ParameterlessCtor : BaseSerializableType_ParameterlessCtor
{
    public int Bar { get; set; }

    public SerializableType_ParameterlessCtor( int foo, int bar )
    {
        Foo = foo;
        Bar = bar;
    }
}

public class SerializableType_DeserializingCtor : BaseSerializableType_DeserializingCtor
{
    public int Bar { get; set; }

    public SerializableType_DeserializingCtor( int foo, int bar )
    {
        Foo = foo;
        Bar = bar;
    }
}

[Inheritable]
public class TestAspect : OverrideMethodAspect
{
    public SerializableType_ParameterlessCtor SerializedValue_ParameterlessCtor;

    public SerializableType_DeserializingCtor SerializedValue_DeserializingCtor;

    public TestAspect( int x, int y )
    {
        SerializedValue_ParameterlessCtor = new SerializableType_ParameterlessCtor( x, y );
        SerializedValue_DeserializingCtor = new SerializableType_DeserializingCtor( x, y );
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.CompileTime( SerializedValue_ParameterlessCtor.Foo ) );
        Console.WriteLine( meta.CompileTime( SerializedValue_ParameterlessCtor.Bar ) );
        Console.WriteLine( meta.CompileTime( SerializedValue_DeserializingCtor.Foo ) );
        Console.WriteLine( meta.CompileTime( SerializedValue_DeserializingCtor.Bar ) );

        return meta.Proceed();
    }
}

public class BaseClass
{
    [TestAspect( 13, 42 )]
    public virtual void Foo() { }
}