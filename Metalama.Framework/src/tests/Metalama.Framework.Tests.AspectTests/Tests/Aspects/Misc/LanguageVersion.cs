// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// The message of LAMA0052 lists the supported language versions, and that list differs between the Roslyn
// variants: the latest variant declares C# 15 and the Roslyn 5.0 variant does not, because a parser accepts only
// a version that its own LanguageVersion declares. One expected file cannot hold both lists, so the scenario is
// split in two, as UnknownAccessorInTemplate is. LanguageVersion_Roslyn5_0 is the counterpart of this test.
// @LanguageVersion(8.0)
// @RequiredConstant(ROSLYN_5_11_0_OR_GREATER)
#endif

using Metalama.Framework.Aspects;

// This reproduces #30669.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.LanguageVersion
{
    internal class MyAspect : TypeAspect { }

    // <target>
    [MyAspect]
    internal class C { }
}