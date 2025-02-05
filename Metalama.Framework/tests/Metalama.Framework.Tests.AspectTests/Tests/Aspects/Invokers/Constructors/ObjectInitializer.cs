using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Constructors.ObjectInitializer;

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
        meta.InsertComment( "Invoke new <target>() { Field = 42, Property = 42 };" );

        var x = target.CreateInvokeExpression()
            .WithObjectInitializer(
                ( target.DeclaringType.Fields.Single( f => !f.IsImplicitlyDeclared ), TypedConstant.Create( 42 ) ),
                ( target.DeclaringType.Properties.Single(), TypedConstant.Create( 42 ) ) )
            .Value;

        return meta.Proceed();
    }
}

// <target>
public class TargetClass
{
    public int Field;

    public int Property { get; init; }

    [InvokerAspect]
    public void Invoker() { }
}