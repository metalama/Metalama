// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
#endif

#if ROSLYN_4_12_0_OR_GREATER

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.PartialProperties_WithImplementation;

public class TheAspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get
        {
            Console.WriteLine("This is aspect code.");

            return meta.Proceed();
        }
        set
        {
            Console.WriteLine("This is aspect code.");

            meta.Proceed();
        }
    }
}

// <target>
internal partial class Target
{
    [TheAspect]
    private partial int P1 { get; set; }

    private partial int P1 { get => 0; set => throw new Exception(); }

    private partial int P2 { get; set; }

    [TheAspect]
    private partial int P2 { get => 0; set => throw new Exception(); }

    [TheAspect]
    private partial int P3 { get; }

    private partial int P3 { get => 0; }

}

#endif