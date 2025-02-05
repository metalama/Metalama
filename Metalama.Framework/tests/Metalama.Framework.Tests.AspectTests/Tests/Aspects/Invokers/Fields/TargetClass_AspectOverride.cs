using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.TargetClass_AspectOverride;
using System.Linq;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(InvokerAfterAspect), typeof(OverrideAspect), typeof(InvokerBeforeAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.TargetClass_AspectOverride;

/*
 * Tests invokers targeting a field declared in the target class which is then overridden by an aspect.
 */

public class InvokerBeforeAspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.OverrideAccessors(
            nameof(GetTemplate),
            nameof(SetTemplate),
            new { target = builder.Target.DeclaringType!.FieldsAndProperties.OfName( "Field" ).Single() } );
    }

    [Template]
    public dynamic? GetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke this.Field" );
        _ = target.Value;
        meta.InsertComment( "Invoke this._field" );
        _ = target.With( InvokerOptions.Base ).Value;
        meta.InsertComment( "Invoke this._field" );
        _ = target.With( InvokerOptions.Current ).Value;
        meta.InsertComment( "Invoke this.Field" );
        _ = target.With( InvokerOptions.Final ).Value;
        meta.InsertComment( "Invoke this._field" );

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke this.Field" );
        target.Value = 42;
        meta.InsertComment( "Invoke this._field" );
        target.With( InvokerOptions.Base ).Value = 42;
        meta.InsertComment( "Invoke this._field" );
        target.With( InvokerOptions.Current ).Value = 42;
        meta.InsertComment( "Invoke this.Field" );
        target.With( InvokerOptions.Final ).Value = 42;
        meta.InsertComment( "Invoke this._field" );
        meta.Proceed();
    }
}

public class OverrideAspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.OverrideAccessors( nameof(GetTemplate), nameof(SetTemplate) );
    }

    [Template]
    public dynamic? GetTemplate()
    {
        meta.InsertComment( "Invoke this._field" );
        _ = meta.Target.FieldOrProperty.Value;
        meta.InsertComment( "Invoke this._field" );
        _ = meta.Target.FieldOrProperty.With( InvokerOptions.Base ).Value;
        meta.InsertComment( "Invoke this.Field" );
        _ = meta.Target.FieldOrProperty.With( InvokerOptions.Current ).Value;
        meta.InsertComment( "Invoke this.Field" );
        _ = meta.Target.FieldOrProperty.With( InvokerOptions.Final ).Value;
        meta.InsertComment( "Invoke this._field" );

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate()
    {
        meta.InsertComment( "Invoke this._field" );
        meta.Target.FieldOrProperty.Value = 42;
        meta.InsertComment( "Invoke this._field" );
        meta.Target.FieldOrProperty.With( InvokerOptions.Base ).Value = 42;
        meta.InsertComment( "Invoke this.Field" );
        meta.Target.FieldOrProperty.With( InvokerOptions.Current ).Value = 42;
        meta.InsertComment( "Invoke this.Field" );
        meta.Target.FieldOrProperty.With( InvokerOptions.Final ).Value = 42;
        meta.InsertComment( "Invoke this._field" );
        meta.Proceed();
    }
}

public class InvokerAfterAspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.OverrideAccessors(
            nameof(GetTemplate),
            nameof(SetTemplate),
            new { target = builder.Target.DeclaringType!.FieldsAndProperties.OfName( "Field" ).Single() } );
    }

    [Template]
    public dynamic? GetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke this.Field" );
        _ = target.Value;
        meta.InsertComment( "Invoke this.Field" );
        _ = target.With( InvokerOptions.Base ).Value;
        meta.InsertComment( "Invoke this.Field" );
        _ = target.With( InvokerOptions.Current ).Value;
        meta.InsertComment( "Invoke this.Field" );
        _ = target.With( InvokerOptions.Final ).Value;
        meta.InsertComment( "Invoke this.Field" );

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke this.Field" );
        target.Value = 42;
        meta.InsertComment( "Invoke this.Field" );
        target.With( InvokerOptions.Base ).Value = 42;
        meta.InsertComment( "Invoke this.Field" );
        target.With( InvokerOptions.Current ).Value = 42;
        meta.InsertComment( "Invoke this.Field" );
        target.With( InvokerOptions.Final ).Value = 42;

        meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    [OverrideAspect]
    public int Field;

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