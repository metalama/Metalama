// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.InvalidCode.DuplicateDeclarationWithAspect;

/*
 * Tests that when there are duplicate declarations, the error is produced without crashing.
 */

public class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        return meta.Proceed();
    }
}

// <target>
internal class TargetCode
{
    [Aspect]
    public int Foo()
    {
        return 42;
    }

#if TESTRUNNER
    [Aspect]
    public int Foo()
    {
        return 42;
    }
#endif
}