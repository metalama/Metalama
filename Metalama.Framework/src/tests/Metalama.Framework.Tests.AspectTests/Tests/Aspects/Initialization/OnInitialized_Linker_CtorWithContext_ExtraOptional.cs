// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Linker_CtorWithContext_ExtraOptional;

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
public class TargetCode
{
    public int Value { get; set; }

    public string? Name { get; set; }

    public TargetCode( int value, string? name = null, InitializationContext context = default )
    {
        this.Value = value;
        this.Name = name;
    }
}

// <target>
public class Caller
{
    public void Method()
    {
        // Only required arg supplied — named arg must skip optional 'name'.
        var t1 = new TargetCode( 42 );

        // Both positional args supplied — named arg appended after 'name'.
        var t2 = new TargetCode( 42, "hello" );
    }
}
