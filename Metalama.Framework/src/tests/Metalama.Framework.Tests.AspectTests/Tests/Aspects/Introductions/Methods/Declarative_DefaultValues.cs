// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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