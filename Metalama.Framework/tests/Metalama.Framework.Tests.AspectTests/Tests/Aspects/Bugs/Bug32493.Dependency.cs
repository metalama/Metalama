using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32493;

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

[RunTimeOrCompileTime]
public class BaseType { }

[RunTimeOrCompileTime]
public class DerivedType : BaseType, ICompileTimeSerializable
{
    public int Value { get; }

    public DerivedType( int value )
    {
        Value = value;
    }
}

public class BaseClass
{
    [TestAspect( 42 )]
    public virtual void Foo()
    {
        Console.WriteLine( "Original" );
    }
}