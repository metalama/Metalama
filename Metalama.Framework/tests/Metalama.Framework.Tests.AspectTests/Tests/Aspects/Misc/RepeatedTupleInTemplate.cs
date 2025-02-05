using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.RepeatedTupleInTemplate;

public class AspectAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Enumerable.Range( 0, 10 ).Select( i => ( i, 0 ) ).Select( x => x.i );
        Enumerable.Range( 0, 10 ).Select( i => ( i, 1 ) ).Select( x => x.i );

        return null;
    }
}

internal class Target
{
    // <target>
    [Aspect]
    private void M() { }
}