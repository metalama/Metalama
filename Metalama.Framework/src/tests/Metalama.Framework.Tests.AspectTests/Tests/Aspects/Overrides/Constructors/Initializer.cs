// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Constructors.Initializer;

/*
 * Tests single OverrideConstructor advice with initializers.
 */

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.Constructors.Single() ).AddInitializer( nameof(InitializerTemplate), args: new { i = 1 } );
        builder.With( builder.Target.Constructors.Single() ).Override( nameof(Template), args: new { i = 1 } );
        builder.With( builder.Target.Constructors.Single() ).AddInitializer( nameof(InitializerTemplate), args: new { i = 2 } );
        builder.With( builder.Target.Constructors.Single() ).Override( nameof(Template), args: new { i = 2 } );
    }

    [Template]
    public void Template( [CompileTime] int i )
    {
        Console.WriteLine( $"This is the override {i}." );
        meta.Proceed();
    }

    [Template]
    public void InitializerTemplate( [CompileTime] int i )
    {
        Console.WriteLine( $"This is the initializer {i}." );
    }
}

// <target>
[Override]
public class TargetClass
{
    public int F = 42;

    public int P { get; } = 42;

    public TargetClass()
    {
        Console.WriteLine( $"This is the original constructor." );
    }
}