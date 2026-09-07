// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(preview)
#endif

using Metalama.Framework.Aspects;

/*
 * A test that requests the preview language version also allows the preview language features, so the expected
 * output is the transformed code. A user project needs the MetalamaAllowPreviewLanguageFeatures MSBuild property
 * beside the LangVersion property, and the LAMA0051 that Metalama reports without it is covered by the
 * PreviewLangVersion scenario of the standalone test suite.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LanguageVersion.LanguageVersionPreview;

public class TheAspect : TypeAspect
{
    [Introduce]
    public string Field;
}

// <target>
[TheAspect]
internal class Target { }