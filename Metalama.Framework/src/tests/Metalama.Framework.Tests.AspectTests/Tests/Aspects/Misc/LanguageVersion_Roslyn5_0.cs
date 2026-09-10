// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// The counterpart of LanguageVersion, for the Roslyn 5.0 variant, whose LAMA0052 message does not list C# 15.
// See the comment of that test for why the scenario is split in two.
// @LanguageVersion(8.0)
// @ForbiddenConstant(ROSLYN_5_11_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;

// This reproduces #30669.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.LanguageVersion_Roslyn5_0
{
    internal class MyAspect : TypeAspect { }

    // <target>
    [MyAspect]
    internal class C { }
}