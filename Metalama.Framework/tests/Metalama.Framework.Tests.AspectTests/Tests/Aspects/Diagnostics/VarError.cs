using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.VarError;

public class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var varDeclaration = meta.RunTime( meta.Target.Declaration );

        return meta.Proceed();
    }
}