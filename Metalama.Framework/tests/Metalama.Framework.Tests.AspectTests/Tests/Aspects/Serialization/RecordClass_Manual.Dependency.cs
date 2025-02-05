using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.RecordClass_Manual;

//<target>
[RunTimeOrCompileTime]
public record class SerializableClass( int Foo ) : ICompileTimeSerializable
{
    public class Serializer_Custom : ReferenceTypeSerializer
    {
        public override object CreateInstance( Type type, IArgumentsReader constructorArguments )
            => new SerializableClass( constructorArguments.GetValue<int>( "Foo" ) );

        public override void DeserializeFields( object obj, IArgumentsReader initializationArguments ) { }

        public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
            => constructorArguments.SetValue( "Foo", ( (SerializableClass)obj ).Foo );
    }
}

[Inheritable]
public class TestAspect : OverrideMethodAspect
{
    public SerializableClass SerializedValue;

    public TestAspect( int x )
    {
        SerializedValue = new SerializableClass( 42 );
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.CompileTime( SerializedValue.Foo ) );

        return meta.Proceed();
    }
}

public class BaseClass
{
    [TestAspect( 42 )]
    public virtual void Foo() { }
}