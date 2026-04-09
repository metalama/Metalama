// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TESTOPTIONS
// @FormatOutput
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Linker_Formatted;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterObjectInitializer );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( "Initialized!" );
    }
}

[TheAspect]
public record TargetRecord( int Value );

[TheAspect]
public class TargetClass
{
    public int Value { get; set; }
}

// <target>
public class Caller
{
    public void Method()
    {
        var t = new TargetClass { Value = 42 };
        var r1 = new TargetRecord( 1 );
        var r2 = r1 with { Value = 2 };
    }
}
