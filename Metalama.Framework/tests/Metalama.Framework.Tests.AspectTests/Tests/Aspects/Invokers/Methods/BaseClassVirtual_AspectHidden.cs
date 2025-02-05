using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassVirtual_AspectHidden;
using System.Linq;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(InvokerAfterAspect), typeof(IntroductionAspect), typeof(InvokerBeforeAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.BaseClassVirtual_AspectHidden;

/*
 * Tests invokers targeting a virtual method declared in the base class which is hidden by an aspect-introduced method.
 */

public class InvokerBeforeAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override(
            nameof(Template),
            new { target = builder.Target.DeclaringType!.BaseType!.Methods.OfName( "Method" ).Single() } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod target )
    {
        meta.InsertComment( "Invoke this.Method" );
        target.Invoke();
        meta.InsertComment( "Invoke base.Method" );
        target.With( InvokerOptions.Base ).Invoke();
        meta.InsertComment( "Invoke base.Method" );
        target.With( InvokerOptions.Current ).Invoke();
        meta.InsertComment( "Invoke this.Method" );
        target.With( InvokerOptions.Final ).Invoke();

        return meta.Proceed();
    }
}

public class IntroductionAspect : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.New )]
    public void Method()
    {
        meta.InsertComment( "Invoke base.Method" );
        meta.Target.Method.Invoke();
        meta.InsertComment( "Invoke base.Method" );
        meta.Target.Method.With( InvokerOptions.Base ).Invoke();
        meta.InsertComment( "Invoke this.Method" );
        meta.Target.Method.With( InvokerOptions.Current ).Invoke();
        meta.InsertComment( "Invoke this.Method" );
        meta.Target.Method.With( InvokerOptions.Final ).Invoke();

        meta.InsertComment( "Invoke base.Method" );
        meta.Proceed();
    }
}

public class InvokerAfterAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override(
            nameof(Template),
            new { target = builder.Target.DeclaringType!.BaseType!.Methods.OfName( "Method" ).Single() } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod target )
    {
        meta.InsertComment( "Invoke this.Method" );
        target.Invoke();
        meta.InsertComment( "Invoke this.Method" );
        target.With( InvokerOptions.Base ).Invoke();
        meta.InsertComment( "Invoke this.Method" );
        target.With( InvokerOptions.Current ).Invoke();
        meta.InsertComment( "Invoke this.Method" );
        target.With( InvokerOptions.Final ).Invoke();

        return meta.Proceed();
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
    [InvokerBeforeAspect]
    public void InvokerBefore() { }

    [InvokerAfterAspect]
    public void InvokerAfter() { }
}