using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Constructors.Generic;

public class InvokerAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.Override(
            nameof(Template),
            new
            {
                target = builder.Target.DeclaringType!.Constructors.Single(),
                target2 = builder.Target.DeclaringType!.MakeGenericInstance( typeof(int) ).Constructors.Single()
            } );
    }

    [Template]
    public dynamic? Template( [CompileTime] IConstructor target, [CompileTime] IConstructor target2 )
    {
        meta.InsertComment( "Invoke new <target><T>();" );
        target.Invoke();

        meta.InsertComment( "Invoke new <target><int>();" );
        target2.Invoke();

        return meta.Proceed();
    }
}

// <target>
public class TargetClass<T>
{
    [InvokerAspect]
    public void Invoker() { }
}