using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.AttributeInitializationWithInheritance;

internal abstract class BaseAttribute : TypeAspect
{
    public abstract int Property { get; set; }
}

internal class IntroductionAttribute : BaseAttribute
{
    [Introduce]
    public override int Property { get; set; }
}

// <target>
[Introduction( Property = 42 )]
internal class TargetClass { }