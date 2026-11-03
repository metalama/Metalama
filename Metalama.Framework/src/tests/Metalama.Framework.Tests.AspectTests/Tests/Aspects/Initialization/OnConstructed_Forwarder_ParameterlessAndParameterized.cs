// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Forwarder_ParameterlessAndParameterized;

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
    public TargetCode()
    {
        this.Name = "Default";
    }

    public TargetCode( string name )
    {
        this.Name = name;
    }

    public string Name { get; }
}

internal class Program
{
    private static T Create<T>()
        where T : new()
        => new T();

    public static void TestMain()
    {
        _ = new TargetCode();
        _ = new TargetCode( "Explicit" );
        _ = Create<TargetCode>();
        _ = Activator.CreateInstance( typeof(TargetCode) );
    }
}
