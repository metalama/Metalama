using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.ArgumentArray;

#pragma warning disable CS0618 // Select is obsolete

public class TestAttribute : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.With( builder.Target.DeclaringType ).IntroduceMethod( nameof(GetMethodInvokerDelegate), args: new { method = builder.Target } );
    }

    [Template]
    public Func<object?, object?[], object?> GetMethodInvokerDelegate( IMethod method )
    {
        return Invoke;

        object? Invoke( object? instance, object?[] args )
        {
            var argExpressions = method.Parameters.Select( p => ExpressionFactory.Capture( args[p.Index]! ) );

            return method.Invoke( argExpressions );
        }
    }
}

// <target>
internal class TargetClass
{
    [Test]
    private int M( int i, int j ) => i + j;
}