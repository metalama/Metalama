using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.MultipleTemplateAttributes;

public class Aspect : TypeAspect
{
    [Introduce]
    [Template]
    private void M() { }
}