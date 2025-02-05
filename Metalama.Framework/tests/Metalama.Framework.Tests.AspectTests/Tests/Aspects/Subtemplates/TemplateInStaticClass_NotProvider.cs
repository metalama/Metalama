using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.TemplateInStaticClass_NotProvider;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );
        StaticClass.StaticTemplate( 1, 2 );

        return default;
    }
}

[RunTimeOrCompileTime]
internal static class StaticClass
{
    [Template]
    public static void StaticTemplate( int i, [CompileTime] int j )
    {
        Console.WriteLine( $"static template i={i}, j={j}" );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}