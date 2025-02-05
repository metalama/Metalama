using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.Parameters_Named;

internal class Aspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(Template), args: new { b = 1, a = 2 } );
        builder.Override( nameof(Template) );
        builder.Override( nameof(Template), args: new { b = 2 } );
    }

    [Template]
    private void Template( [CompileTime] int a = -1, [CompileTime] int b = -2 )
    {
        Console.WriteLine( $"template a={a} b={b}" );
        meta.Proceed();
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void M() { }
}