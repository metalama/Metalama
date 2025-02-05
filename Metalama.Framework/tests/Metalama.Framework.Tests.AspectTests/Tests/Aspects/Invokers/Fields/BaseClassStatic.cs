using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.BaseClassStatic;

/*
 * Tests invokers targeting a static field declared in the base class.
 */

public class InvokerAspect : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.OverrideAccessors(
            nameof(GetTemplate),
            nameof(SetTemplate),
            new { target = builder.Target.DeclaringType!.BaseType!.FieldsAndProperties.OfName( "Field" ).Single() } );
    }

    [Template]
    public dynamic? GetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke BaseClass.Field" );
        _ = target.Value;
        meta.InsertComment( "Invoke BaseClass.Field" );
        _ = target.With( InvokerOptions.Base ).Value;
        meta.InsertComment( "Invoke BaseClass.Field" );
        _ = target.With( InvokerOptions.Current ).Value;
        meta.InsertComment( "Invoke BaseClass.Field" );
        _ = target.With( InvokerOptions.Final ).Value;

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke BaseClass.Field" );
        target.Value = 42;
        meta.InsertComment( "Invoke BaseClass.Field" );
        target.With( InvokerOptions.Base ).Value = 42;
        meta.InsertComment( "Invoke BaseClass.Field" );
        target.With( InvokerOptions.Current ).Value = 42;
        meta.InsertComment( "Invoke BaseClass.Field" );
        target.With( InvokerOptions.Final ).Value = 42;

        meta.Proceed();
    }
}

public class BaseClass
{
    public static int Field;
}

// <target>
public class TargetClass : BaseClass
{
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