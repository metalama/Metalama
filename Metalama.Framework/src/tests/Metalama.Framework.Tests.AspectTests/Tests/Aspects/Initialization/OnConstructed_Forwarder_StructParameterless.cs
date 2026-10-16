// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Forwarder_StructParameterless;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterLastInstanceConstructor );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( $"OnConstructed {meta.Target.Type.Name}" );
    }
}

// <target>
[TheAspect]
public struct ExplicitParameterless
{
    public ExplicitParameterless()
    {
        this.Value = 1;
    }

    public int Value { get; }
}

// <target>
[TheAspect]
public struct ImplicitParameterless
{
    public ImplicitParameterless( int value )
    {
        this.Value = value;
    }

    public int Value { get; }
}

internal class Program
{
    public static void TestMain()
    {
        // The explicit parameterless constructor must still run, and OnConstructed must run once.
        Console.WriteLine( $"Value={new ExplicitParameterless().Value}" );

        // A struct without an explicit parameterless constructor is initialized to its default value without running a constructor.
        Console.WriteLine( $"Value={new ImplicitParameterless().Value}" );
        Console.WriteLine( $"Value={new ImplicitParameterless( 2 ).Value}" );
    }
}
