#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

#if NET6_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects; 

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.RefField_NotEligible;

public class TheAspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty { get; set; }
}

internal ref struct S
{
    [TheAspect]
    private ref int x;
}

#endif