using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.DirectCallWithArgs;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );
        CalledTemplate( 42 );

        return default;
    }

    [Template]
    private void CalledTemplate( [CompileTime] int i )
    {
        Console.WriteLine( $"called template i={i}" );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}