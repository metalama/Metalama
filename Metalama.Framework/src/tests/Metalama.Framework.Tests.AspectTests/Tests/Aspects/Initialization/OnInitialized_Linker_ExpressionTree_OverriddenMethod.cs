// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// Object creation expressions inside an expression tree are not rewritten when the containing method is overridden
// by an aspect, whether the source body is inlined or moved to another method. The warning is reported on the
// object creation expression of the source code. The same rule applies to a hand-written IInitializable type.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.RunTime.Initialization;
using System;
using System.Linq.Expressions;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnInitialized_Linker_ExpressionTree_OverriddenMethod;

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

public class HandWritten : IInitializable
{
    public virtual void Initialize( InitializationContext context )
    {
        Console.WriteLine( "Initialized HandWritten" );
    }
}

public class LogAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Calling {meta.Target.Method.Name}" );

        return meta.Proceed();
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

// <target>
public class Caller
{
    [LogAspect]
    public Expression<Func<TargetCode>> Inlined()
    {
        return () => new TargetCode { Value = 1 };
    }

    [ProceedTwiceAspect]
    public Expression<Func<TargetCode>> NotInlined()
    {
        return () => new TargetCode();
    }

    public Expression<Func<HandWritten>> HandWrittenInitializable()
    {
        return () => new HandWritten();
    }
}

internal class Program
{
    public static void TestMain()
    {
        var caller = new Caller();
        Console.WriteLine( "Inlined" );
        caller.Inlined().Compile()();
        Console.WriteLine( "NotInlined" );
        caller.NotInlined().Compile()();
        Console.WriteLine( "HandWrittenInitializable" );
        caller.HandWrittenInitializable().Compile()();
    }
}
