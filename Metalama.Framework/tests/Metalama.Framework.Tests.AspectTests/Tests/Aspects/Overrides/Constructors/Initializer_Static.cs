// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Constructors.Initializer_Static;

/*
 * Tests single OverrideConstructor advice on a static constructor with static initializers.
 */

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), kind: InitializerKind.BeforeTypeConstructor, args: new { i = 1 } );
        builder.With( builder.Target.StaticConstructor! ).Override( nameof(Template), args: new { i = 1 } );
        builder.AddInitializer( nameof(InitializerTemplate), kind: InitializerKind.BeforeTypeConstructor, args: new { i = 2 } );
        builder.With( builder.Target.StaticConstructor! ).Override( nameof(Template), args: new { i = 2 } );
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
    public static int F = 42;

    public static int P { get; } = 42;

    static TargetClass()
    {
        Console.WriteLine( $"This is the original constructor." );
    }
}