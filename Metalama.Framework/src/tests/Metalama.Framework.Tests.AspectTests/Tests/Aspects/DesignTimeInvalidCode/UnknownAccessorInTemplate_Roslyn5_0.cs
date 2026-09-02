// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @ForbiddenConstant(ROSLYN_5_10_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.DesignTimeInvalidCode.UnknownAccessorInTemplate_Roslyn5_0;

/// <summary>
/// Tests that an invalid accessor declaration in a template does not crash the design-time pipeline.
/// </summary>
/// <remarks>
/// This is the Roslyn 5.0 counterpart of <c>UnknownAccessorInTemplate</c>. The two tests differ only in their
/// expected output: Roslyn 5.0 reports <c>CS1014</c> on an empty span, and Roslyn 5.10 reports it on the
/// <c>setx</c> token. The test framework compares a single expected file per test, so the scenario needs one
/// test per Roslyn variant.
/// </remarks>
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
