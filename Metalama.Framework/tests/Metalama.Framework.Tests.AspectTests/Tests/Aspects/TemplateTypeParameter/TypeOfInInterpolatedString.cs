using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameters.TypeOfInInterpolatedString;

#pragma warning disable CS0219

public class Aspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(Method), args: new { T = builder.Target.ReturnType } );
    }

    [Template]
    private T Method<[CompileTime] T>() where T : class
    {
        Console.WriteLine( $"{typeof(T)}" );
        Console.WriteLine( $"{typeof(List<T>)}" );
        Console.WriteLine( $"{typeof(Target)}" );
        Console.WriteLine( $"{typeof(string)}" );

        // Not sure what should happen here. Seems to be an unimportant side case.
        Console.WriteLine( $"{typeof(IMethod)}" );

        return null!;
    }
}

// <target>
public class Target
{
    [Aspect]
    public string M() => "";
}