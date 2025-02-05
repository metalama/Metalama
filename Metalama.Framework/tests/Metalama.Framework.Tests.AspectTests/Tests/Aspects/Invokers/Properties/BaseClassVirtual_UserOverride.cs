using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.BaseClassVirtual_UserOverride;

/*
 * Tests invokers targeting a virtual property declared in the base class which is overridden by a C# property.
 */

public class InvokerAspect : PropertyAspect
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
        meta.InsertComment( "Invoke this.Property_Source" );
        _ = target.With( InvokerOptions.Base ).Value;
        meta.InsertComment( "Invoke this.Property_Source" );
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
        meta.InsertComment( "Invoke this.Property_Source" );
        target.With( InvokerOptions.Base ).Value = 42;
        meta.InsertComment( "Invoke this.Property_Source" );
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
public class TargetClass : BaseClass
{
    public override int Property
    {
        get
        {
            return 0;
        }
        set { }
    }

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