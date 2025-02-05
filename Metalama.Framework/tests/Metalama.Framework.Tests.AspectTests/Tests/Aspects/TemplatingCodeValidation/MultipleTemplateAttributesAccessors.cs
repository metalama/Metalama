using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.MultipleTemplateAttributesAccessors;

public class Aspect : TypeAspect
{
    [Template]
    private int P
    {
        [Template]
        get => 42;
    }
}