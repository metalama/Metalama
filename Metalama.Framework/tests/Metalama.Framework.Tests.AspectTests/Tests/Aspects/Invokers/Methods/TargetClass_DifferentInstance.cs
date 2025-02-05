using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.TargetClass_DifferentInstance;

/*
 * Tests default and final invokers targeting a method declared in a different class.
 */

public class InvokerAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override(
            nameof(Template),
            new { target = builder.Target.DeclaringType!.Methods.OfName( "Method" ).Single() } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IMethod target )
    {
        meta.InsertComment( "Invoke instance.Method" );
        target.With( (IExpression)meta.Target.Method.Parameters[0].Value! ).Invoke();
        meta.InsertComment( "Invoke instance?.Method" );
        target.With( (IExpression)meta.Target.Method.Parameters[0].Value!, InvokerOptions.NullConditional ).Invoke();
        meta.InsertComment( "Invoke instance.Method" );
        target.With( (IExpression)meta.Target.Method.Parameters[0].Value!, InvokerOptions.Final ).Invoke();
        meta.InsertComment( "Invoke instance?.Method" );
        target.With( (IExpression)meta.Target.Method.Parameters[0].Value!, InvokerOptions.Final | InvokerOptions.NullConditional ).Invoke();

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    public void Method() { }

    [InvokerAspect]
    public void Invoker( TargetClass instance ) { }
}