using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.MultipleTemplateAttributesInheritance;

public class Aspect : OverrideMethodAspect
{
    [Introduce]
    public override dynamic? OverrideMethod()
    {
        return null;
    }
}