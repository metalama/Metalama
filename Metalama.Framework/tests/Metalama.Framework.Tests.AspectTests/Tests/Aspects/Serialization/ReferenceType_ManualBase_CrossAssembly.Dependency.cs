using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.ReferenceType_ManualBase_CrossAssembly;

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