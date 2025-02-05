using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Virtual_OptionalParameters;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );

        CalledTemplate( 1, 2 );
        CalledTemplate( 1 );
        CalledTemplate();

        return meta.Proceed();
    }

    [Template]
    protected virtual void CalledTemplate( int i = -1, [CompileTime] int j = -2 )
    {
        Console.WriteLine( $"called template i={i} j={j}" );
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method() { }
}