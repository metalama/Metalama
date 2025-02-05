using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.RecordClass_NonPositional;

//<target>
[RunTimeOrCompileTime]
public record class SerializableClass : ICompileTimeSerializable
{
    public int Foo { get; set; }
}

[Inheritable]
public class TestAspect : OverrideMethodAspect
{
    public SerializableClass SerializedValue;

    public TestAspect( int x )
    {
        SerializedValue = new SerializableClass() { Foo = 42 };
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