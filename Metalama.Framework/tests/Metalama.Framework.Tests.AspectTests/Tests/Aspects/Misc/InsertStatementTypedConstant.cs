using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Misc.InsertStatementTypedConstant;

public class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.InsertStatement( TypedConstant.Create( 42 ) );

        return null;
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void M() { }
}