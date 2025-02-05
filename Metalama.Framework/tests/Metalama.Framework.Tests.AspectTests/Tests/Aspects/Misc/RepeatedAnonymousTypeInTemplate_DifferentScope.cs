using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.RepeatedAnonymousTypeInTemplate_DifferentScope;

public class AspectAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Enumerable.Range( 0, 10 ).Select( i => new { i } );
        meta.CompileTime( Enumerable.Range( 0, 10 ).Select( i => new { i } ) );

        return null;
    }
}

internal class Target
{
    // <target>
    [Aspect]
    private void M() { }
}