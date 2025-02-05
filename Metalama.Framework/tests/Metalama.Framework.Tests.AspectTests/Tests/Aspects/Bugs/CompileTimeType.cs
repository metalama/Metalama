using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.CompileTimeType;

public class Aspect1 : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        var version = typeof(IAspectBuilder).Assembly.GetName().Name;

        builder.Override( nameof(OverrideMethod), args: new { versionFromBuildAspect = version } );
    }

    [Template]
    private dynamic? OverrideMethod( [CompileTime] string versionFromBuildAspect )
    {
        var version = meta.CompileTime( typeof(IAspectBuilder).Assembly.GetName().Name );
        Console.WriteLine( $"Aspect1: {version}, {versionFromBuildAspect}" );

        return meta.Proceed();
    }
}

// <target>
internal class C
{
    [Aspect1]
    public void M() { }
}