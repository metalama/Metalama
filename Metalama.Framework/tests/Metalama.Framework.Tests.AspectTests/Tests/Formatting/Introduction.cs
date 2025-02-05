using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.Introduction;

public partial class C
{
    private void M()
    {
        #if METALAMA
        this.IntroducedMethod();
        #endif
    }

    class Fabric : TypeFabric
    {
        [Introduce]
        public void IntroducedMethod() { }
    }
}