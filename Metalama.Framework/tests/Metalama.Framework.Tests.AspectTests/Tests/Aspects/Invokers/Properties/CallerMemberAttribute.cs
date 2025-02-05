using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;
using System.Runtime.CompilerServices;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.CallerMemberAttribute;

[assembly: AspectOrder(AspectOrderDirection.CompileTime, typeof(OverrideAspect), typeof(InvokerAspect))]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.CallerMemberAttribute;

/*
 * Tests that invokers targeting a non-overridden property that calls a method with [CallerMemberAttribute] are not rewritten.
 */

public class InvokerAspect : TypeAspect
{
    [Introduce]
    public void CallFoo()
    {
        foreach (var property in meta.Target.Type.Properties)
        {
            property.Value = 42;
            property.With(InvokerOptions.Base).Value = 42;
            property.With(InvokerOptions.Current).Value = 42;
            property.With(InvokerOptions.Final).Value = 42;
        }

        meta.Proceed();
    }
}

public class OverrideAspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get => meta.Proceed();
        set
        {
            if (meta.Target.Property.Name == nameof(TargetClass.TestOverriddenProperty))
            {
                meta.Proceed();
            }
            else
            {
                meta.Proceed();
                meta.Proceed();
            }
        }
    }
}

public class OtherClass
{
    public static void Foo([CallerMemberName] string? callerMemberName = null )
    {
    }
}

// <target>
[InvokerAspect]
public class TargetClass
{
    public int TestProperty
    {
        get { return 0; }
        set { OtherClass.Foo(); }
    }

    [OverrideAspect]
    public int TestOverriddenProperty
    {
        get { return 0; }
        set { OtherClass.Foo(); }
    }

    [OverrideAspect]
    public int TestOverriddenNonInlinedProperty
    {
        get { return 0; }
        set { OtherClass.Foo(); }
    }
}
