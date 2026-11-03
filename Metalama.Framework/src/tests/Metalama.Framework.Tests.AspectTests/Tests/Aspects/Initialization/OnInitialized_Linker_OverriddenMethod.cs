// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Linker_OverriddenMethod;

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

public class UsingAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        using ( new Scope() )
        {
            return meta.Proceed();
        }
    }
}

public class ProceedTwiceAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        if ( Environment.ProcessorCount > 0 )
        {
            return meta.Proceed();
        }

        return meta.Proceed();
    }
}

public class CreatingAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        _ = new TargetCode();

        return meta.Proceed();
    }
}

public sealed class Scope : IDisposable
{
    public void Dispose() { }
}

// <target>
public class Caller
{
    [UsingAspect]
    public TargetCode Inlined()
    {
        return new TargetCode { Value = 1 };
    }

    [UsingAspect]
    [ProceedTwiceAspect]
    public TargetCode TwoAspects()
    {
        return new TargetCode();
    }

    [ProceedTwiceAspect]
    public TargetCode NotInlined()
    {
        return new TargetCode();
    }

    [CreatingAspect]
    public void CreatedByTemplate()
    {
    }

    public TargetCode NotOverridden()
    {
        return new TargetCode();
    }
}

internal class Program
{
    public static void TestMain()
    {
        var caller = new Caller();
        Console.WriteLine( "Inlined" );
        caller.Inlined();
        Console.WriteLine( "TwoAspects" );
        caller.TwoAspects();
        Console.WriteLine( "NotInlined" );
        caller.NotInlined();
        Console.WriteLine( "CreatedByTemplate" );
        caller.CreatedByTemplate();
        Console.WriteLine( "NotOverridden" );
        caller.NotOverridden();
    }
}
