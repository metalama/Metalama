// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_SemiAutoProperty_AutoGetter;

/// <summary>
/// A property declared with the <c>field</c> keyword whose getter returns the backing field, either because the getter is
/// automatic or because its body reads <c>field</c> and nothing else. Reading the property is then reading the backing
/// field, so the materialized <c>Equals</c> agrees with the compiler-synthesized one and nothing is reported.
/// </summary>
public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedEquals), whenExists: OverrideStrategy.Override, args: new { T = builder.Target } );
    }

    [Template( Name = "Equals" )]
    public bool IntroducedEquals<[CompileTime] T>( T? other )
    {
        return meta.Proceed();
    }
}

// <target>
[Override]
internal record Transformed
{
    public int Validated
    {
        get;

        set
        {
            if ( value < 0 )
            {
                throw new ArgumentOutOfRangeException( nameof(value) );
            }

            field = value;
        }
    }

    public int Read
    {
        get => field;
        set => field = value;
    }
}

internal record Twin
{
    public int Validated
    {
        get;

        set
        {
            if ( value < 0 )
            {
                throw new ArgumentOutOfRangeException( nameof(value) );
            }

            field = value;
        }
    }

    public int Read
    {
        get => field;
        set => field = value;
    }
}

internal static class Program
{
    public static void TestMain()
    {
        var a = new Transformed { Validated = 1, Read = 2 };
        var b = new Transformed { Validated = 1, Read = 2 };
        var c = new Transformed { Validated = 1, Read = 3 };

        var twinA = new Twin { Validated = 1, Read = 2 };
        var twinC = new Twin { Validated = 1, Read = 3 };

        Console.WriteLine( $"Equals(same): {a.Equals( b )}" );
        Console.WriteLine( $"Equals(different): {a.Equals( c )}" );
        Console.WriteLine( $"MatchesCompiler(same): {a.Equals( b ) == twinA.Equals( new Twin { Validated = 1, Read = 2 } )}" );
        Console.WriteLine( $"MatchesCompiler(different): {a.Equals( c ) == twinA.Equals( twinC )}" );
    }
}
