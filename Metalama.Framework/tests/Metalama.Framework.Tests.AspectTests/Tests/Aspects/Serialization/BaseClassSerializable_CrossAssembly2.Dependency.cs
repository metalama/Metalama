using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.BaseClassSerializable_CrossAssembly2;

[RunTimeOrCompileTime]
public class DerivedType : MiddleType
{
    public int Value { get; }

    public DerivedType( int baseValue, int middleValue, int value ) : base( baseValue, middleValue )
    {
        Value = value;
    }
}

[Inheritable]
public class TestAspect : OverrideMethodAspect
{
    public DerivedType SerializedValue;

    public TestAspect( int x, int y, int z )
    {
        SerializedValue = new DerivedType( x, y, z );
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.CompileTime( SerializedValue.BaseValue ) );
        Console.WriteLine( meta.CompileTime( SerializedValue.MiddleValue ) );
        Console.WriteLine( meta.CompileTime( SerializedValue.Value ) );

        return meta.Proceed();
    }
}

public class BaseClass
{
    [TestAspect( 13, 27, 42 )]
    public virtual void Foo() { }
}