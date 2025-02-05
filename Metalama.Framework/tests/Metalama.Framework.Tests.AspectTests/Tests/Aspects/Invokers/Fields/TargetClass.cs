using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.TargetClass;

/*
 * Tests invokers targeting a field declared in the target class.
 */

public class InvokerAspect : FieldOrPropertyAspect
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
    public int Field;

    [InvokerAspect]
    public int Invoker
    {
        get
        {
            return 0;
        }
        set { }
    }
}