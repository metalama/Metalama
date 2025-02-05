using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.IntroducedOverride;
using System.Linq;

[assembly: AspectOrder(AspectOrderDirection.CompileTime, typeof(IntroductionAspect), typeof(InvokerAspect))]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.IntroducedOverride;

/*
 * Tests invokers targeting a method introduced into the target class by previous aspect.
 */

public class InvokerAspect : MethodAspect
{
    public override void BuildAspect(IAspectBuilder<IMethod> builder)
    {
        builder.Override(
            nameof(Template),
            new { target = builder.Target.DeclaringType!.Methods.OfName("Method").Single() });
    }

    [Template]
    public dynamic? Template([CompileTime] IMethod target)
    {
        meta.InsertComment("Invoke this.Method");
        target.With(InvokerOptions.Base).Invoke();
        meta.InsertComment("Invoke this.Method");
        target.With(InvokerOptions.Current).Invoke();
        meta.InsertComment("Invoke this.Method");
        target.With(InvokerOptions.Final).Invoke();

        return meta.Proceed();
    }
}

public class IntroductionAspect : TypeAspect
{
    [Introduce(WhenExists = OverrideStrategy.Override)]
    public void Method()
    {
    }
}

public class BaseClass
{
    public virtual void Method() { }
}

// <target>
[IntroductionAspect]
public class TargetClass : BaseClass
{
    [InvokerAspect]
    public void Invoker() { }
}