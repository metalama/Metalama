// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Record_WithExpressionGap;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterLastInstanceConstructor );
    }

    [Template]
    private void InitializerTemplate()
    {
        TargetRecord.Counter++;
    }
}

// <target>
[TheAspect]
public sealed record TargetRecord( int X )
{
    public static int Counter;
}

internal class Program
{
    public static void Main()
    {
        // Primary ctor: runs OnConstructed once -> Counter becomes 1.
        var r = new TargetRecord( 1 );

        // `with` flows through the compiler-generated copy ctor, which Metalama cannot
        // modify. Counter stays at 1 — this is the documented gap.
        _ = r with { X = 2 };

        Console.WriteLine( $"Counter={TargetRecord.Counter}" );
    }
}
