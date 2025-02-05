using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Fabrics.DeclarativeAdvice;

public class C
{
    private class F : TypeFabric
    {
        [Introduce]
        private void M() { }
    }
}