using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.AutomaticPropertyTemplateUnexpected;

public class TheAspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty { get; set; }
}

// <target>
internal class C
{
    [TheAspect]
    private int P { get; set; }
}