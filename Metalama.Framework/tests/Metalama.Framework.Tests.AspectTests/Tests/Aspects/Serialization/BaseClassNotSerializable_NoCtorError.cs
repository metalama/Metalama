using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.BaseClassNotSerializable_NoCtorError;

/*
 * The base class of a serializable typeis not itself serializable and does not have a parameterless base constructor.
 */

[RunTimeOrCompileTime]
public class BaseType
{
    public int BaseValue { get; }

    public BaseType( int baseValue )
    {
        BaseValue = 13;
    }
}

[RunTimeOrCompileTime]
public class DerivedType : BaseType, ICompileTimeSerializable
{
    public int Value { get; }

    public DerivedType( int value, int baseValue ) : base( baseValue )
    {
        Value = value;
    }
}