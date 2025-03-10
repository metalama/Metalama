// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.UnknownAccessor;

/*
 * Tests that invalid accessor declarations do not crash.
 */

internal class Aspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get
        {
            return meta.Proceed();
        }

        set
        {
            meta.Proceed();
        }
    }
}

// <target>
internal class TargetCode
{
#if TESTRUNNER
    [Aspect]
    public int Foo 
    { 
        getx;
    }

    [Aspect]
    public int Bar
    { 
        ;
    }
#endif
}