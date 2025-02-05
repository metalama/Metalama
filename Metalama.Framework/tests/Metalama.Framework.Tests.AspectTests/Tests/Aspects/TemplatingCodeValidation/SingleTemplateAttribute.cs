using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.SingleTemplateAttribute;

// Checks that template attribute that's accessible through associated symbol -> overriden symbol
// and also though overriden symbol -> associated symbol is not considered twice for the purpose of LAMA0261.

public class Aspect : TypeAspect
{
    [Template]
    public virtual int P
    {
        get => 42;
    }
}

public class DerivedAspect : Aspect
{
    public override int P => 0;
}

// <target>
[Aspect]
internal class TargetCode { }