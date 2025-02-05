using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.RecordStruct_NonPositional;

//<target>
[RunTimeOrCompileTime]
public record struct SerializableStruct : ICompileTimeSerializable
{
    public int Foo { get; set; }

    public class Serializer_Custom : ValueTypeSerializer<SerializableStruct>
    {
        public override SerializableStruct DeserializeObject( IArgumentsReader initializationArguments )
        {
            SerializableStruct s = default;
            s.Foo = initializationArguments.GetValue<int>( "Foo" );

            return s;
        }

        public override void SerializeObject( SerializableStruct obj, IArgumentsWriter arguments )
            => arguments.SetValue( "Foo", ( (SerializableStruct)obj ).Foo );
    }
}

[Inheritable]
public class TestAspect : OverrideMethodAspect
{
    public SerializableStruct SerializedValue;

    public TestAspect( int x )
    {
        SerializedValue = new SerializableStruct() { Foo = 42 };
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