// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Linker_ExpressionBodiedProperty;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterObjectInitializer );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( $"Initialized {meta.Target.Type.Name}" );
    }
}

[TheAspect]
public class TargetCode
{
    public int Value { get; set; }
}

// <target>
public class Caller
{
    public TargetCode Property => new TargetCode();

    public TargetCode this[ int index ] => new TargetCode { Value = index };

    public TargetCode Method() => new TargetCode();
}

internal class Program
{
    public static void TestMain()
    {
        var caller = new Caller();
        Console.WriteLine( "Property" );
        _ = caller.Property;
        Console.WriteLine( "Indexer" );
        _ = caller[1];
        Console.WriteLine( "Method" );
        _ = caller.Method();
    }
}
