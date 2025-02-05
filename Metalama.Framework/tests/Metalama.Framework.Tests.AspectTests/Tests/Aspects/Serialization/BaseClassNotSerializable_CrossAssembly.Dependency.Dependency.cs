using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.BaseClassNotSerializable_CrossAssembly;

[RunTimeOrCompileTime]
public class BaseType
{
    public int BaseValue { get; }

    public BaseType()
    {
        BaseValue = 13;
    }
}