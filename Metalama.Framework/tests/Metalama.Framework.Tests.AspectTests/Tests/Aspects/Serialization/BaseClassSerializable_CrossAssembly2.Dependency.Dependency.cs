using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Serialization.BaseClassSerializable_CrossAssembly2;

[RunTimeOrCompileTime]
public class MiddleType : BaseType
{
    public int MiddleValue { get; }

    public MiddleType( int baseValue, int middleValue ) : base( baseValue )
    {
        MiddleValue = middleValue;
    }
}