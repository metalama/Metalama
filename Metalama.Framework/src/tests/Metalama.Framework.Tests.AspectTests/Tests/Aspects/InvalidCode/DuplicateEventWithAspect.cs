// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TESTOPTIONS
// @RequiredConstant(NET8_0_OR_GREATER)
// @Skipped(#1257 - Non-deterministic output ordering)
#endif

using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.DuplicateEventWithAspect;

/*
 * Tests that when there are duplicate declarations, the error is produced without crashing.
 */

internal class Aspect : OverrideEventAspect
{
    public override void OverrideAdd( dynamic handler )
    {
        Console.WriteLine( "Aspect" );

        meta.Proceed();
    }

    public override void OverrideRemove( dynamic handler )
    {
        Console.WriteLine( "Aspect" );

        meta.Proceed();
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    private event EventHandler? Event;

#if TESTRUNNER
    [Aspect]
    private event EventHandler? Event;
#endif
}
