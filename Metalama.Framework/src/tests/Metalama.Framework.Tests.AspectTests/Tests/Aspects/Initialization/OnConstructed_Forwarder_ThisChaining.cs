// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Forwarder_ThisChaining;

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
public class TargetCode
{
    public TargetCode() : this( 42 )
    {
    }

    public TargetCode( int value )
    {
        this.Value = value;
    }

    public int Value { get; }
}

internal class Program
{
    public static void TestMain()
    {
        var instance = (TargetCode) Activator.CreateInstance( typeof(TargetCode) )!;
        Console.WriteLine( $"Value={instance.Value}" );
        _ = new TargetCode( 1 );
    }
}
