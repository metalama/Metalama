using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.ExistingConflictOverrideBaseNonVirtual
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int BaseProperty
        {
            get => meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public static int BaseProperty_Static
        {
            get => meta.Proceed();
        }
    }

    internal class BaseClass
    {
        public int BaseProperty
        {
            get => 13;
        }

        public static int BaseProperty_Static
        {
            get => 13;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass : BaseClass { }
}