using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.DifferentClass_Base_Error;

/*
 * Tests that base invoker targeting a method declared in a different class produces an error.
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
        target.With( (IExpression)meta.Target.Method.Parameters[0].Value!, InvokerOptions.Base ).Invoke();

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