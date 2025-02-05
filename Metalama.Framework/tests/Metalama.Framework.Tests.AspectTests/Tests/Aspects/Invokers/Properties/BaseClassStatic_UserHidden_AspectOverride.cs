using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride;
using System.Linq;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(InvokerAfterAspect), typeof(OverrideAspect), typeof(InvokerBeforeAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassStatic_UserHidden_AspectOverride;

/*
 * Tests invokers targeting a virtual property declared in the base class, which is hidden by a C# property which is then overridden by an aspect.
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
        meta.InsertComment( "Invoke TargetClass.Property" );
        _ = target.Value;
        meta.InsertComment( "Invoke TargetClass.Property_Source" );
        _ = target.With( InvokerOptions.Base ).Value;
        meta.InsertComment( "Invoke TargetClass.Property_Source" );
        _ = target.With( InvokerOptions.Current ).Value;
        meta.InsertComment( "Invoke TargetClass.Property" );
        _ = target.With( InvokerOptions.Final ).Value;

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IProperty target )
    {
        meta.InsertComment( "Invoke TargetClass.Property" );
        target.Value = 42;
        meta.InsertComment( "Invoke TargetClass.Property_Source" );
        target.With( InvokerOptions.Base ).Value = 42;
        meta.InsertComment( "Invoke TargetClass.Property_Source" );
        target.With( InvokerOptions.Current ).Value = 42;
        meta.InsertComment( "Invoke TargetClass.Property" );
        target.With( InvokerOptions.Final ).Value = 42;

        meta.Proceed();
    }
}

public class OverrideAspect : PropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IProperty> builder )
    {
        builder.OverrideAccessors( nameof(GetTemplate), nameof(SetTemplate) );
    }

    [Template]
    public dynamic? GetTemplate()
    {
        meta.InsertComment( "Invoke TargetClass.Property_Source" );
        _ = meta.Target.Property.Value;
        meta.InsertComment( "Invoke TargetClass.Property_Source" );
        _ = meta.Target.Property.With( InvokerOptions.Base ).Value;
        meta.InsertComment( "Invoke TargetClass.Property" );
        _ = meta.Target.Property.With( InvokerOptions.Current ).Value;
        meta.InsertComment( "Invoke TargetClass.Property" );
        _ = meta.Target.Property.With( InvokerOptions.Final ).Value;
        meta.InsertComment( "Invoke TargetClass.Property_Source" );

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate()
    {
        meta.InsertComment( "Invoke TargetClass.Property_Source" );
        meta.Target.Property.Value = 42;
        meta.InsertComment( "Invoke TargetClass.Property_Source" );
        meta.Target.Property.With( InvokerOptions.Base ).Value = 42;
        meta.InsertComment( "Invoke TargetClass.Property" );
        meta.Target.Property.With( InvokerOptions.Current ).Value = 42;
        meta.InsertComment( "Invoke TargetClass.Property" );
        meta.Target.Property.With( InvokerOptions.Final ).Value = 42;
        meta.InsertComment( "Invoke TargetClass.Property_Source" );
        meta.Proceed();
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
        meta.InsertComment( "Invoke TargetClass.Property" );
        _ = target.Value;
        meta.InsertComment( "Invoke TargetClass.Property" );
        _ = target.With( InvokerOptions.Base ).Value;
        meta.InsertComment( "Invoke TargetClass.Property" );
        _ = target.With( InvokerOptions.Current ).Value;
        meta.InsertComment( "Invoke TargetClass.Property" );
        _ = target.With( InvokerOptions.Final ).Value;
        meta.InsertComment( "Invoke TargetClass.Property" );

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IProperty target )
    {
        meta.InsertComment( "Invoke TargetClass.Property" );
        target.Value = 42;
        meta.InsertComment( "Invoke TargetClass.Property" );
        target.With( InvokerOptions.Base ).Value = 42;
        meta.InsertComment( "Invoke TargetClass.Property" );
        target.With( InvokerOptions.Current ).Value = 42;
        meta.InsertComment( "Invoke TargetClass.Property" );
        target.With( InvokerOptions.Final ).Value = 42;

        meta.Proceed();
    }
}

public class BaseClass
{
    public static int Property
    {
        get
        {
            return 0;
        }
        set { }
    }
}

// <target>
public class TargetClass : BaseClass
{
    [OverrideAspect]
    public new static int Property
    {
        get
        {
            return 0;
        }
        set { }
    }

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