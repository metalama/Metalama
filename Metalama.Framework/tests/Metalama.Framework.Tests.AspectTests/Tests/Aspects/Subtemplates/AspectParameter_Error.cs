using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.AspectParameter_Error;

internal class Aspect : OverrideMethodAspect
{
    public Aspect( int i )
    {
        I = i;
    }

    public int I { get; }

    public override dynamic? OverrideMethod()
    {
        var aspect = this;
        AnotherClass.CalledTemplate( aspect );
        AnotherClass.CalledTemplate( this );

        return default;
    }
}

internal class AnotherClass : ITemplateProvider
{
    [Template]
    public static void CalledTemplate( Aspect aspect )
    {
        Console.WriteLine( $"called template i={aspect.I}" );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect( 42 )]
    private void Method() { }
}