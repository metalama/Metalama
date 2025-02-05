using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameters.TypeOf;

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
        // Try all possible type constructs.

        var t7 = meta.RunTime( typeof(T) );
        var t8 = meta.RunTime( typeof(List<T>) );

        return null!;
    }
}

// <target>
public class Target
{
    [Aspect]
    public string M() => "";
}