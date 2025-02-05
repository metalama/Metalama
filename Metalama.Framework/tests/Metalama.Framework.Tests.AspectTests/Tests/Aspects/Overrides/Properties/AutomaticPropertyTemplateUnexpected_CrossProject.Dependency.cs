using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.AutomaticPropertyTemplateUnexpected_CrossProject;

public class TheAspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty { get; set; }
}