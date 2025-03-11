// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.Unsafe;

public unsafe class Aspect1 : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        void* ptr = null;

        return meta.Proceed();
    }
}

public class Aspect2 : OverrideMethodAspect
{
    public override unsafe dynamic? OverrideMethod()
    {
        void* ptr = null;

        return meta.Proceed();
    }

    [Introduce]
    public unsafe int* Property => null;
}