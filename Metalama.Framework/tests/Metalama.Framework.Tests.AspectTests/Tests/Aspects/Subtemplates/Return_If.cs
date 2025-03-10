// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Return_If;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        CalledTemplate( false );
        CalledTemplate( true );

        return default;
    }

    [Template]
    private void CalledTemplate( [CompileTime] bool shouldReturn )
    {
        Console.WriteLine( $"Shold return? {shouldReturn}" );

        if (shouldReturn)
        {
            return;
        }
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private void Method() { }
}