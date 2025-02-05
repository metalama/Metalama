using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Constructors.Arguments;

public class InvokerAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override(
            nameof(Template),
            new { target = builder.Target.DeclaringType!.Constructors.Single() } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IConstructor target )
    {
        meta.InsertComment( "Invoke new <target>();" );
        target.Invoke( 42, new object() );

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    public TargetClass( int x, object y ) { }

    [InvokerAspect]
    public void Invoker() { }
}