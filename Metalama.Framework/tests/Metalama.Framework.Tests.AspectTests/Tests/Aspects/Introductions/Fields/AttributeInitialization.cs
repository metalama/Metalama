using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.AttributeInitialization;

public class IntroductionAttribute : TypeAspect
{
    [Introduce]
    public int IntroducedField;
}

// <target>
[Introduction( IntroducedField = 42 )]
internal class TargetClass { }