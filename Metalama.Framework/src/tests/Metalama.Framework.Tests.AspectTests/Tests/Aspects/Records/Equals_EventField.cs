// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.Equals_EventField;

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
internal record Target
{
    public int X;

    public event EventHandler? Changed;

    public void Raise() => this.Changed?.Invoke( this, EventArgs.Empty );
}

internal static class Program
{
    public static void TestMain()
    {
        var a = new Target { X = 1 };
        var b = new Target { X = 1 };

        Console.WriteLine( $"NoHandler: {a.Equals( b )}" );

        a.Changed += Handler;

        // The C# compiler compares the backing field of a field-like event in the synthesized Equals, so two records
        // that have the same X but different subscriptions are not equal.
        Console.WriteLine( $"OneHandler: {a.Equals( b )}" );

        b.Changed += Handler;

        Console.WriteLine( $"SameHandler: {a.Equals( b )}" );

        static void Handler( object? sender, EventArgs args ) { }
    }
}
