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