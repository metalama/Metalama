using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Constructors.Static_Error;

public class InvokerAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override(
            nameof(Template),
            new { target = builder.Target.DeclaringType!.StaticConstructor } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IConstructor target )
    {
        meta.InsertComment( "Invoke new <target>();" );
        target.Invoke();

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    static TargetClass() { }

    [InvokerAspect]
    public void Invoker() { }
}