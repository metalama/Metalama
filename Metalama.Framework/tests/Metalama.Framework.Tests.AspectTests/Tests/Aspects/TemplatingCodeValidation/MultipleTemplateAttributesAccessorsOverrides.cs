using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.MultipleTemplateAttributesAccessorsOverrides;

public class BaseAspect : TypeAspect
{
    [Template]
    public virtual int P
    {
        get => 42;
    }
}

public class Aspect : BaseAspect
{
    [Template]
    public override int P
    {
        get => 42;
    }
}