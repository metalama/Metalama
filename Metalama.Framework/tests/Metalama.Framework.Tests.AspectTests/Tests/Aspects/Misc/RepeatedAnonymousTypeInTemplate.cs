using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.RepeatedAnonymousTypeInTemplate;

public class AspectAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Enumerable.Range( 0, 10 ).Select( i => new { i } ).Select( x => x.i );
        Enumerable.Range( 0, 10 ).Select( i => new { i } ).Select( x => x.i );

        return null;
    }
}

internal class Target
{
    // <target>
    [Aspect]
    private void M() { }
}