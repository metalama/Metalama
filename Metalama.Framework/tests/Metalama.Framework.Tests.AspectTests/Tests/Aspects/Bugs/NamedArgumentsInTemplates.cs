using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.NamedArgumentsInTemplates;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        meta.Target.Method.With( (IExpression)meta.This, InvokerOptions.Default ).Invoke();

        meta.Target.Method.With( target: (IExpression)meta.This, options: InvokerOptions.Default ).Invoke();

        meta.Target.Method.With( options: InvokerOptions.Default, target: (IExpression)meta.This ).Invoke();

        return null;
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void M() { }
}