// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
# endif

using Metalama.Framework.Aspects;
using System.ComponentModel;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.DesignTimeInvalidCode.IncompleteEventInvocation;

class Aspect : OverrideMethodAspect
{
    [Introduce]
    public event PropertyChangedEventHandler? PropertyChanged1;

    [Introduce]
    public PropertyChangedEventHandler? PropertyChanged2;

    public override dynamic? OverrideMethod()
    {
#if TESTRUNNER
        this.PropertyChanged1(meta.This, );
        this.PropertyChanged2(meta.This, );
#endif

        return meta.Proceed();
    }
}

class Target
{
    [Aspect]
    void M()
    {
    }
}