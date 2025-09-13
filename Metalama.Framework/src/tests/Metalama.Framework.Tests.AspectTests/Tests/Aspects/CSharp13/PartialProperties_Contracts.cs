// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
#endif

#if ROSLYN_4_12_0_OR_GREATER

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.PartialProperties_Contracts;

public class TheAspect : ContractAspect
{
    public override void Validate(dynamic? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(meta.Target.Declaration.ToString());
        }
    }
}

// <target>
internal partial class Target
{
    [TheAspect]
    private partial string P1 { get; set; }

    private partial string P1 { get => "foo"; set => throw new Exception(); }

    private partial string P2 { get; set; }

    [TheAspect]
    private partial string P2 { get => "foo"; set => throw new Exception(); }

    [TheAspect]
    private partial string P3 { get; }

    private partial string P3 { get => "foo"; }
}

#endif