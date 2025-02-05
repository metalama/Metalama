using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.ExistingConflictOverride_Error
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int ExistingField;
    }

    internal class BaseClass
    {
        public int ExistingField;
    }

    // <target>
    [Introduction]
    internal class TargetClass : BaseClass { }
}