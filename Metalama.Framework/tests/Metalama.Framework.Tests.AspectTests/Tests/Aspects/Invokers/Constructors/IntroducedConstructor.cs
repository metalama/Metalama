using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Constructors.IntroducedConstructor;

public class InvokerAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        var c = builder.With( builder.Target.DeclaringType! )
            .IntroduceConstructor(
                nameof(ConstructorTemplate),
                buildConstructor: b => { b.AddParameter( "x", typeof(int) ); } );

        builder.Override(
            nameof(Template),
            new { target = c.Declaration } );
    }

    [Template]
    public void ConstructorTemplate() { }

    [Template]
    public dynamic? Template( [CompileTime] IConstructor target )
    {
        meta.InsertComment( "Invoke new <target>(42);" );
        target.Invoke( 42 );

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    [InvokerAspect]
    public void Invoker() { }
}