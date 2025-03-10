// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @SkipAddingSystemFiles
// @RequiredConstant(NETFRAMEWORK)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.GetOnlyAutopropertyOverride_NetFramework;

public sealed class TestAspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get
        {
            Console.WriteLine( "getter" );

            return meta.Proceed();
        }
        set
        {
            Console.WriteLine( "setter" );

            meta.Proceed();
        }
    }
}

// <target>
public class Target
{
    [TestAspect]
    public int Test { get; }
}