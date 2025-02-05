using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Fields.Introduced_SameAspect;

/*
 * Tests invokers targeting a field introduced by the same aspect.
 */

public class InvokerAspect : TypeAspect
{
    [Introduce]
    public int Field;

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.With(builder.Target.Properties.OfName("Invoker").Single()).OverrideAccessors(
            nameof(GetTemplate),
            nameof(SetTemplate),
            new { target = builder.Target.ForCompilation(builder.Advice.MutableCompilation).Fields.OfName("Field").Single() });
    }

    [Template]
    public dynamic? GetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke instance.Empty_Field" );
        _ = target.With( InvokerOptions.Base ).Value;
        meta.InsertComment( "Invoke instance.Field" );
        _ = target.With( InvokerOptions.Current ).Value;
        meta.InsertComment( "Invoke instance.Field" );
        _ = target.With( InvokerOptions.Final ).Value;

        return meta.Proceed();
    }

    [Template]
    public void SetTemplate( [CompileTime] IFieldOrProperty target )
    {
        meta.InsertComment( "Invoke instance.Empty_Field" );
        target.With( InvokerOptions.Base ).Value = 42;
        meta.InsertComment( "Invoke instance.Field" );
        target.With( InvokerOptions.Current ).Value = 42;
        meta.InsertComment( "Invoke instance.Field" );
        target.With( InvokerOptions.Final ).Value = 42;

        meta.Proceed();
    }
}

// <target>
[InvokerAspect]
public class TargetClass
{
    public int Invoker
    {
        get
        {
            return 0;
        }
        set { }
    }
}