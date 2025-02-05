using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.AspectParameter;

internal class Aspect : OverrideMethodAspect
{
    public Aspect( int i )
    {
        I = i;
    }

    public int I { get; }

    public override dynamic? OverrideMethod()
    {
        var aspect = meta.RunTime( new Aspect( I ) );
        AnotherClass.RunTimeAspect( aspect );
        AnotherClass.CompileTimeAspect( this );

        return default;
    }
}

internal class AnotherClass : ITemplateProvider
{
    [Template]
    public static void RunTimeAspect( Aspect aspect )
    {
        Console.WriteLine( $"run-time i={aspect.I}" );
    }

    [Template]
    public static void CompileTimeAspect( [CompileTime] Aspect aspect )
    {
        Console.WriteLine( $"compile-time i={aspect.I}" );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect( 42 )]
    private void Method() { }
}