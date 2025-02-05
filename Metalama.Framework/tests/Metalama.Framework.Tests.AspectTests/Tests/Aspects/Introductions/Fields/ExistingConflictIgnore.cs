using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.ExistingConflictIgnore
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Ignore )]
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