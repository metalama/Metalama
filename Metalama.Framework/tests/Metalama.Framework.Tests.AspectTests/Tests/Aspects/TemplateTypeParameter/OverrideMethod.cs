using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateTypeParameters.OverrideMethod;

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
        var x = default(T);
        var t = (T)null!;
        var t2 = (T?)null;
        var t3 = (List<T>)null!;
        var t4 = (T[])null!;
        var t5 = (List<T[]>)null!;
        var t6 = (List<T>[])null!;

        return Target.GenericMethod<T>( (T)meta.Proceed()! );
    }
}

// <target>
public class Target
{
    [Aspect]
    public string M() => "";

    public static T GenericMethod<T>( T x ) => x;
}