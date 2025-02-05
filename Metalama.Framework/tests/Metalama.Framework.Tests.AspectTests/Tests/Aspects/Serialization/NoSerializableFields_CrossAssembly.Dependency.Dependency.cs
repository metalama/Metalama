using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.NoSerializableFields_CrossAssembly;

[RunTimeOrCompileTime]
public class BaseType : ICompileTimeSerializable
{
    public int BaseValue { get; }

    public ValueContainer BaseContainer { get; }

    public BaseType( int baseValue )
    {
        BaseValue = baseValue;
        BaseContainer = new ValueContainer( baseValue );
    }
}

[RunTimeOrCompileTime]
public class ValueContainer : ICompileTimeSerializable
{
    public int Value { get; }

    public ValueContainer( int value )
    {
        Value = value;
    }
}