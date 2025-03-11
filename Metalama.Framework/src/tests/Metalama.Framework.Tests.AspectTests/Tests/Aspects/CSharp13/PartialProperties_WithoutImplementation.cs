// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
#endif

#if ROSLYN_4_12_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.PartialProperties_WithoutImplementation;

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
partial class Target
{
#if TESTRUNNER
    [TheAspect]
    partial int P1 { get; set; }

    [TheAspect]
    partial int P2 { get; }
#endif
}

#endif