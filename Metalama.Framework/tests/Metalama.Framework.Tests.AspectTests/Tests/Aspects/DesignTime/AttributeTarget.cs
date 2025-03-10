// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @FormatOutput
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.AttributeTarget;

public class IntroductionAttribute : TypeAspect
{
    [Test]
    [field: Test]
    [Introduce]
    public int M { get; set; }
}

[RunTimeOrCompileTime]
public class TestAttribute : Attribute { }

// <target>
[Introduction]
internal partial class TargetClass { }