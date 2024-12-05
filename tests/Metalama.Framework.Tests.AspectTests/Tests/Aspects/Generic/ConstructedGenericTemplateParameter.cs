using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Generic.ConstructedGenericTemplateParameter;

public class Aspect : MethodAspect
{
    public override void BuildAspect(IAspectBuilder<IMethod> builder)
    {
        base.BuildAspect(builder);

        builder.Advice.Override(builder.Target, nameof(Template), new { T = typeof(int) });
    }

    [Template]
    private dynamic Template<[CompileTime] T>(T[] arg)
    {
        Console.WriteLine(arg[0]);

        return meta.Proceed();
    }
}

// <target>
class TargetCode
{
    [Aspect]
    public int M(int[] arg)
    {
        return 0;
    }
}