using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.TargetClass_DifferentInstance_Base;

/*
 * Tests base invoker targeting a property from a different instance.
 */

public class InvokerAspect : PropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IProperty> builder )
    {
        builder.OverrideAccessors(
            nameof(GetTemplate),
            nameof(SetTemplate),
            new { target = ( (INamedType)builder.Target.DeclaringType.Fields.Single().Type ).Properties.OfName( "Property" ).Single() } );
    }

    [Template]
    public dynamic? GetTemplate( [CompileTime] IProperty target )
    {
        _ = target.With( (IExpression)meta.Target.Property.DeclaringType.Fields.Single().Value!, InvokerOptions.Base ).Value;

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IProperty target )
    {
        target.With( (IExpression)meta.Target.Property.DeclaringType.Fields.Single().Value!, InvokerOptions.Base ).Value = 42;

        meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    public int Property
    {
        get
        {
            return 0;
        }
        set { }
    }

    private TargetClass? instance;

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