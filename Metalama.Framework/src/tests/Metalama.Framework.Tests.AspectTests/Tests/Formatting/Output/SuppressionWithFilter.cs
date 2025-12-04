// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @ClearIgnoredDiagnostics
#endif

#if !TESTRUNNER // Disable the warning in the main build, not during tests.
#pragma warning disable CS0414
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.Output.SuppressionWithFilter;

[SuppressFiltered]
public class C
{
    // This field triggers CS0414 (assigned but never used).
    // The suppression filter only matches "_initialized", so _other should still warn.
    private int _initialized = 1;
    private int _other = 2;
}
