using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.BaseClassSerializable_CrossAssembly2;

[RunTimeOrCompileTime]
public class BaseType : ICompileTimeSerializable
{
    public int BaseValue { get; }

    public BaseType( int baseValue )
    {
        BaseValue = baseValue;
    }
}