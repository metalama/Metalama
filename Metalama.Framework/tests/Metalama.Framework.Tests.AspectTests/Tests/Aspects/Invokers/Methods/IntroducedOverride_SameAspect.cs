using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.IntroducedOverride_SameAspect;

/*
 * Tests invokers targeting a method introduced into the target class by the same aspect.
 */

public class InvokerAspect : TypeAspect
{
    [Introduce(WhenExists = OverrideStrategy.Override)]
    public void Method()
    {
    }

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.With(builder.Target.Methods.OfName("Invoker").Single()).Override(
            nameof(Template),
            new { target = builder.Target.ForCompilation(builder.Advice.MutableCompilation).Methods.OfName("Method").Single() });
    }

    [Template]
    public dynamic? Template([CompileTime] IMethod target)
    {
        meta.InsertComment("Invoke base.Method");
        target.With(InvokerOptions.Base).Invoke();
        meta.InsertComment("Invoke this.Method");
        target.With(InvokerOptions.Current).Invoke();
        meta.InsertComment("Invoke this.Method");
        target.With(InvokerOptions.Final).Invoke();

        return meta.Proceed();
    }
}

public class BaseClass
{
    public virtual void Method() {}
}

// <target>
[InvokerAspect]
public class TargetClass : BaseClass
{
    public void Invoker() { }
}