using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.ExistingConflictNew_DifferentKind
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int ExistingField;
    }

    internal class BaseClass
    {
        public int ExistingField { get; set; }
    }

    // <target>
    [Introduction]
    internal class TargetClass : BaseClass { }
}