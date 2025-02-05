using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DifferentClass;

/*
 * Tests default and final invokers targeting a method declared in a different class.
 */

public class InvokerAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override(
            nameof(Template),
            new { target = ( (INamedType)builder.Target.Parameters[0].Type ).Methods.OfName( "Method" ).Single() } );
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

public class DifferentClass
{
    public void Method() { }
}

// <target>
public class TargetClass
{
    [InvokerAspect]
    public void Invoker( DifferentClass instance ) { }
}