// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @ForbiddenConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.DesignTimeInvalidCode.UnknownAccessorInTemplate_Roslyn4;

/*
 * Tests that invalid accessor declarations in a template do not crash.
 *
 * This is the Roslyn 4 counterpart of UnknownAccessorInTemplate. The two files differ only in the
 * expected output: Roslyn 4.12 reports CS1014 on an empty span, and Roslyn 5.10 reports it on the
 * `setx` token. The test framework compares a single expected file per test, so the scenario needs
 * one file per Roslyn variant.
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
