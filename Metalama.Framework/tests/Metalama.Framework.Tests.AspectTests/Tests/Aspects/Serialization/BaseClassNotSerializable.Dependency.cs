using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.BaseClassNotSerializable;

[RunTimeOrCompileTime]
public class BaseType
{
    public int BaseValue { get; }

    public BaseType()
    {
        BaseValue = 13;
    }
}

[RunTimeOrCompileTime]
public class DerivedType : BaseType, ICompileTimeSerializable
{
    public int Value { get; }

    public DerivedType( int value )
    {
        Value = value;
    }
}

[Inheritable]
public class TestAspect : OverrideMethodAspect
{
    public DerivedType SerializedValue;

    public TestAspect( int z )
    {
        SerializedValue = new DerivedType( z );
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.CompileTime( SerializedValue.Value ) );

        return meta.Proceed();
    }
}

public class BaseClass
{
    [TestAspect( 42 )]
    public virtual void Foo() { }
}