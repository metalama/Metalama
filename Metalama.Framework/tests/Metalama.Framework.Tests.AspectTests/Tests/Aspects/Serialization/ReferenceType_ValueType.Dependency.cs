using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.ReferenceType_ValueType;

[RunTimeOrCompileTime]
public class ReferenceType : ICompileTimeSerializable
{
    public ValueType Value;

    public ReferenceType( int value )
    {
        Value.Value = value;
    }
}

[RunTimeOrCompileTime]
public struct ValueType : ICompileTimeSerializable
{
    public int Value { get; set; }
}

[Inheritable]
public class TestAspect : OverrideMethodAspect
{
    public ReferenceType SerializedValue;

    public TestAspect( int x )
    {
        SerializedValue = new ReferenceType( x );
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.CompileTime( SerializedValue.Value.Value ) );

        return meta.Proceed();
    }
}

public class BaseClass
{
    [TestAspect( 42 )]
    public virtual void Foo() { }
}