using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.DifferentClass;

/*
 * Tests default and final invokers targeting a property declared in a different class.
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
        meta.InsertComment( "Invoke instance.Property" );
        _ = target.With( (IExpression?)meta.Target.Property.DeclaringType.Fields.Single().Value ).Value;
        meta.InsertComment( "Invoke instance?.Property" );
        _ = target.With( (IExpression?)meta.Target.Property.DeclaringType.Fields.Single().Value, InvokerOptions.NullConditional ).Value;
        meta.InsertComment( "Invoke instance.Property" );
        _ = target.With( (IExpression?)meta.Target.Property.DeclaringType.Fields.Single().Value, InvokerOptions.Final ).Value;
        meta.InsertComment( "Invoke instance?.Property" );
        _ = target.With( (IExpression?)meta.Target.Property.DeclaringType.Fields.Single().Value, InvokerOptions.Final | InvokerOptions.NullConditional ).Value;

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IProperty target )
    {
        meta.InsertComment( "Invoke instance.Property" );
        target.With( (IExpression?)meta.Target.Property.DeclaringType.Fields.Single().Value ).Value = 42;
        meta.InsertComment( "Invoke instance.Property" );
        target.With( (IExpression?)meta.Target.Property.DeclaringType.Fields.Single().Value, InvokerOptions.Final ).Value = 42;

        meta.Proceed();
    }
}

public class DifferentClass
{
    public int Property
    {
        get
        {
            return 0;
        }
        set { }
    }
}

// <target>
public class TargetClass
{
    private DifferentClass? instance;

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