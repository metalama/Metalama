// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Linker_ThrowExpressionBody;

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

    public override string ToString() => "TargetCode";
}

// <target>
public class Caller
{
    public int Property => throw new InvalidOperationException( new TargetCode().ToString() );

    public int this[ int index ] => throw new InvalidOperationException( new TargetCode { Value = index }.ToString() );

    public int Method() => throw new InvalidOperationException( new TargetCode().ToString() );

    public void VoidMethod() => throw new InvalidOperationException( new TargetCode().ToString() );
}

internal class Program
{
    private static void Run( string name, Action action )
    {
        try
        {
            action();
        }
        catch ( InvalidOperationException e )
        {
            Console.WriteLine( $"{name}: {e.Message}" );
        }
    }

    public static void TestMain()
    {
        var caller = new Caller();
        Run( "Property", () => _ = caller.Property );
        Run( "Indexer", () => _ = caller[1] );
        Run( "Method", () => _ = caller.Method() );
        Run( "VoidMethod", () => caller.VoidMethod() );
    }
}
