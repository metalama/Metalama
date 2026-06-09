// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.FieldKeyword_Override_SemiAutoTarget;

// Overriding a semi-automatic property (C# 14 'field' keyword + explicit accessor body) must invoke the original
// accessor body via meta.Proceed(), not silently drop it (regression test for #1644).
internal class LoggingAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var property = builder.Target.Properties.Single( p => p.Name == "Value" );

        builder.With( property ).Override( nameof(PropertyTemplate) );
    }

    [Template]
    public int PropertyTemplate
    {
        get
        {
            Console.WriteLine( "Getting" );

            return meta.Proceed();
        }
        set
        {
            Console.WriteLine( $"Setting to {value}" );
            meta.Proceed();
        }
    }
}

// <target>
[LoggingAspect]
internal class C
{
    // Semi-automatic property whose manual setter has a side effect.
    public int Value
    {
        get => field;
        set
        {
            field = value;
            this.SetterCallCount++;
        }
    }

    public int SetterCallCount { get; private set; }
}

internal static class Program
{
    private static void TestMain()
    {
        var c = new C();
        c.Value = 42;

        Console.WriteLine( $"Value={c.Value} (expected 42)" );
        Console.WriteLine( $"SetterCallCount={c.SetterCallCount} (expected 1)" );

        if ( c.Value != 42 || c.SetterCallCount != 1 )
        {
            throw new InvalidOperationException(
                $"The original semi-auto setter body was not executed: Value={c.Value} (expected 42), SetterCallCount={c.SetterCallCount} (expected 1)." );
        }

        Console.WriteLine( "Execution OK." );
    }
}

#endif
