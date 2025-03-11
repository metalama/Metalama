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

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.PartialIndexers_Contracts;

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
partial class Target
{
    [TheAspect]
    partial string this[int i] { get; set; }

    partial string this[int i] { get => "foo"; set => throw new Exception(); }

    partial string this[string s] { get; set; }

    [TheAspect]
    partial string this[string s] { get => "foo"; set => throw new Exception(); }

    [TheAspect]
    partial string this[long i] { get; }

    partial string this[long i] { get => "foo"; }
}

#endif