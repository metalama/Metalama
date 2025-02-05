using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.OverrideMethod;

public class Aspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(Method), args: new { a = 5, t = builder.Target.ReturnType } );
    }

    [Template]
    private void Method( [CompileTime] int a, IType t )
    {
        Console.WriteLine( a );
        Console.WriteLine( t.ToDisplayString() );
    }
}

// <target>
public class Target
{
    [Aspect]
    public void M() { }
}