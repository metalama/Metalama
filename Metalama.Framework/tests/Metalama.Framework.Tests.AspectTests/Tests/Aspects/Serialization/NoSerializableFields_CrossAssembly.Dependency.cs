using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.NoSerializableFields_CrossAssembly;

[RunTimeOrCompileTime]
public class MiddleType : BaseType
{
    public MiddleType( int baseValue ) : base( baseValue ) { }
}

[RunTimeOrCompileTime]
public class DerivedType : MiddleType
{
    public DerivedType( int baseValue ) : base( baseValue ) { }
}

[Inheritable]
public class TestAspect : OverrideMethodAspect
{
    public DerivedType SerializedValue;

    public TestAspect( int x )
    {
        SerializedValue = new DerivedType( x );
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( meta.CompileTime( SerializedValue.BaseValue ) );
        Console.WriteLine( meta.CompileTime( SerializedValue.BaseContainer.Value ) );

        return meta.Proceed();
    }
}

public class BaseClass
{
    [TestAspect( 42 )]
    public virtual void Foo() { }
}