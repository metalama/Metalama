// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.MixedIf;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        IFieldOrProperty? logger = null;

        if (logger != null && logger.Value != null)
        {
            logger!.Value!.ToString();
        }

        return null;
    }
}

internal class TargetCode
{
    [Aspect]
    private int Method( int a )
    {
        return a;
    }
}