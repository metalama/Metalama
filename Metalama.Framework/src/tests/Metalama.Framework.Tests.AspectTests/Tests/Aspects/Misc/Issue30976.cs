// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#define YES

#if TEST_OPTIONS
// @KeepDisabledCode
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue30976;

/*
 * Tests that MyAspect is wrapped with `#pragma warning`, but it does
not interfere with the #if/#endif directives above the aspect.
 */

#if YES

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );
    }
}

#endif

public class X { }

// Note that end-of-file trivia are dropped by the testing framework, 
// so we use the X class to force the next trivia to be preserved.