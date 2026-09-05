// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Text;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Records.PrintMembers_DiscardedProceed;

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(IntroducedPrintMembers), whenExists: OverrideStrategy.Override );
    }

    [Template( Name = "PrintMembers" )]
    protected bool IntroducedPrintMembers( StringBuilder builder )
    {
        _ = meta.Proceed();

        return true;
    }
}

internal record BaseRecord( int X );

// <target>
[Override]
internal record DerivedRecord : BaseRecord
{
    public DerivedRecord( int x ) : base( x ) { }
}

internal static class Program
{
    public static void TestMain()
    {
        // The original implementation of PrintMembers of a derived record that has no printable member of its own is
        // the call to the base implementation, which appends the members of the base record to the builder. The
        // template discards the result of meta.Proceed, so the call must still be executed for X to be printed.
        Console.WriteLine( new DerivedRecord( 1 ).ToString() );
    }
}
