using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.AttributeInitialization;

internal class IntroductionAttribute : TypeAspect
{
    [Introduce]
    public int IntroducedProperty { get; set; }
}

// <target>
[Introduction( IntroducedProperty = 42 )]
internal class TargetClass { }