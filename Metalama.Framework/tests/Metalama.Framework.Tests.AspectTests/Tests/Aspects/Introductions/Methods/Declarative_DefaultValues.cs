using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Declarative_DefaultValues;

public class IntroductionAttribute : TypeAspect
{
    [Introduce]
    public int IntroducedMethod_StringLiteral( string x = "a" )
    {
        Console.WriteLine( $"This is introduced method, x = {x}." );

        return meta.Proceed();
    }

    [Introduce]
    public int IntroducedMethod_StringNullLiteral( string? x = null )
    {
        Console.WriteLine( $"This is introduced method, x = {x}." );

        return meta.Proceed();
    }

    [Introduce]
    public int IntroducedMethod_IntLiteral( int x = 27 )
    {
        Console.WriteLine( $"This is introduced method, x = {x}." );

        return meta.Proceed();
    }

    [Introduce]
    public int IntroducedMethod_DefaultLiteral( int x = default )
    {
        Console.WriteLine( $"This is introduced method, x = {x}." );

        return meta.Proceed();
    }

    [Introduce]
    public int IntroducedMethod_DecimalLiteral( decimal x = 3.14m )
    {
        Console.WriteLine( $"This is introduced method, x = {x}." );

        return meta.Proceed();
    }
}

// <target>
[Introduction]
internal class TargetClass { }