using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassVirtual_AspectHidden;
using System.Linq;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(InvokerAfterAspect), typeof(IntroductionAspect), typeof(InvokerBeforeAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassVirtual_AspectHidden;

/*
 * Tests invokers targeting a virtual property declared in the base class which is hidden by an aspect-introduced property.
 */

public class InvokerBeforeAspect : PropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IProperty> builder )
    {
        builder.OverrideAccessors(
            nameof(GetTemplate),
            nameof(SetTemplate),
            new { target = builder.Target.DeclaringType!.BaseType!.Properties.OfName( "Property" ).Single() } );
    }

    [Template]
    public dynamic? GetTemplate( [CompileTime] IProperty target )
    {
        meta.InsertComment( "Invoke this.Property" );
        _ = target.Value;
        meta.InsertComment( "Invoke base.Property" );
        _ = target.With( InvokerOptions.Base ).Value;
        meta.InsertComment( "Invoke base.Property" );
        _ = target.With( InvokerOptions.Current ).Value;
        meta.InsertComment( "Invoke this.Property" );
        _ = target.With( InvokerOptions.Final ).Value;

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IProperty target )
    {
        meta.InsertComment( "Invoke this.Property" );
        target.Value = 42;
        meta.InsertComment( "Invoke base.Property" );
        target.With( InvokerOptions.Base ).Value = 42;
        meta.InsertComment( "Invoke base.Property" );
        target.With( InvokerOptions.Current ).Value = 42;
        meta.InsertComment( "Invoke this.Property" );
        target.With( InvokerOptions.Final ).Value = 42;

        meta.Proceed();
    }
}

public class IntroductionAspect : TypeAspect
{
    [Introduce( WhenExists = OverrideStrategy.New )]
    public int Property
    {
        get
        {
            meta.InsertComment( "Invoke base.Property" );
            _ = meta.Target.Property.Value;
            meta.InsertComment( "Invoke base.Property" );
            _ = meta.Target.Property.With( InvokerOptions.Base ).Value;
            meta.InsertComment( "Invoke this.Property" );
            _ = meta.Target.Property.With( InvokerOptions.Current ).Value;
            meta.InsertComment( "Invoke this.Property" );
            _ = meta.Target.Property.With( InvokerOptions.Final ).Value;
            meta.InsertComment( "Invoke base.Property" );

            return meta.Proceed();
        }

        set
        {
            meta.InsertComment( "Invoke base.Property" );
            meta.Target.Property.Value = 42;
            meta.InsertComment( "Invoke base.Property" );
            meta.Target.Property.With( InvokerOptions.Base ).Value = 42;
            meta.InsertComment( "Invoke this.Property" );
            meta.Target.Property.With( InvokerOptions.Current ).Value = 42;
            meta.InsertComment( "Invoke this.Property" );
            meta.Target.Property.With( InvokerOptions.Final ).Value = 42;
            meta.InsertComment( "Invoke base.Property" );
            meta.Proceed();
        }
    }
}

public class InvokerAfterAspect : PropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IProperty> builder )
    {
        builder.OverrideAccessors(
            nameof(GetTemplate),
            nameof(SetTemplate),
            new { target = builder.Target.DeclaringType!.BaseType!.Properties.OfName( "Property" ).Single() } );
    }

    [Template]
    public dynamic? GetTemplate( [CompileTime] IProperty target )
    {
        meta.InsertComment( "Invoke this.Property" );
        _ = target.Value;
        meta.InsertComment( "Invoke this.Property" );
        _ = target.With( InvokerOptions.Base ).Value;
        meta.InsertComment( "Invoke this.Property" );
        _ = target.With( InvokerOptions.Current ).Value;
        meta.InsertComment( "Invoke this.Property" );
        _ = target.With( InvokerOptions.Final ).Value;

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IProperty target )
    {
        meta.InsertComment( "Invoke this.Property" );
        target.Value = 42;
        meta.InsertComment( "Invoke this.Property" );
        target.With( InvokerOptions.Base ).Value = 42;
        meta.InsertComment( "Invoke this.Property" );
        target.With( InvokerOptions.Current ).Value = 42;
        meta.InsertComment( "Invoke this.Property" );
        target.With( InvokerOptions.Final ).Value = 42;

        meta.Proceed();
    }
}

public class BaseClass
{
    public virtual int Property
    {
        get
        {
            return 0;
        }
        set { }
    }
}

// <target>
[IntroductionAspect]
public class TargetClass : BaseClass
{
    [InvokerBeforeAspect]
    public int InvokerBefore
    {
        get
        {
            return 0;
        }
        set { }
    }

    [InvokerAfterAspect]
    public int InvokerAfter
    {
        get
        {
            return 0;
        }
        set { }
    }
}