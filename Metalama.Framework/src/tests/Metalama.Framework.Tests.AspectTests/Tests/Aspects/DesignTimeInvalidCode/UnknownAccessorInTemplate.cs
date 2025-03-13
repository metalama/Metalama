// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.DesignTimeInvalidCode.UnknownAccessorInTemplate;

/*
 * Tests that invalid accessor declarations in a template do not crash.
 */

internal class Aspect : PropertyAspect
{
    [Template]
    public dynamic? Template
    {
        get
        {
            return meta.Proceed();
        }

#if TESTRUNNER
        setx
        {
            meta.Proceed();
        }
#endif
    }
}