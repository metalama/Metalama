// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Text;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.ToString_OverriddenPrintMembers;

/// <summary>
/// The aspect overrides both synthesized members that call each other. The C# compiler calls <c>PrintMembers</c> from
/// <c>ToString</c>, so the materialized <c>ToString</c> reaches the advice on <c>PrintMembers</c>, in the same way as
/// a <c>ToString</c> written in source would.
/// </summary>
public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedToString), whenExists: OverrideStrategy.Override );
        builder.IntroduceMethod( nameof(IntroducedPrintMembers), whenExists: OverrideStrategy.Override );
    }

    [Template( Name = "ToString" )]
    public string IntroducedToString()
    {
        return meta.Proceed()!;
    }

    [Template( Name = "PrintMembers" )]
    protected bool IntroducedPrintMembers( StringBuilder builder )
    {
        var result = meta.Proceed();
        builder.Append( ", Suffix = 1" );

        return result;
    }
}

// <target>
[Override]
internal record Target( int X );

internal static class Program
{
    public static void TestMain()
    {
        Console.WriteLine( new Target( 1 ).ToString() );
    }
}
