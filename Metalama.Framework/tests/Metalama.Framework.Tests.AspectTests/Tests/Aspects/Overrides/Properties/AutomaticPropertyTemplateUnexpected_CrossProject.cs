namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.AutomaticPropertyTemplateUnexpected_CrossProject;

// <target>
internal class C
{
    [TheAspect]
    private int P { get; set; }
}