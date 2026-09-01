// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.DesignTimeInvalidCode.UnknownAccessorInTemplate;

/// <summary>
/// Tests that an invalid accessor declaration in a template does not crash the design-time pipeline.
/// </summary>
/// <remarks>
/// This test runs on the latest Roslyn variant only. <c>UnknownAccessorInTemplate_Roslyn4</c> is its counterpart
/// for the Roslyn 4.12 variant, which reports the same error on a different span.
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