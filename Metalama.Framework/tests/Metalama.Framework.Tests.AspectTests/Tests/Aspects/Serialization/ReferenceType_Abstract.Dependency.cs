using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.ReferenceType_Abstract;

[RunTimeOrCompileTime]
public abstract class AbstractBaseType : ICompileTimeSerializable
{
    public int BaseValue { get; }

    public AbstractBaseType( int value )
    {
        BaseValue = value;
    }
}

[RunTimeOrCompileTime]
public class ReferenceType : AbstractBaseType
{
    public int Value { get; }

    public ReferenceType( int value ) : base( value )
    {
        Value = value;
    }
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
        Console.WriteLine( meta.CompileTime( SerializedValue.BaseValue ) );
        Console.WriteLine( meta.CompileTime( SerializedValue.Value ) );

        return meta.Proceed();
    }
}

public class BaseClass
{
    [TestAspect( 42 )]
    public virtual void Foo() { }
}