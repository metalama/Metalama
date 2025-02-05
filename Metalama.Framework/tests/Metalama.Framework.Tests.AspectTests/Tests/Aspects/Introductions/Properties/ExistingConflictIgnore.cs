using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.ExistingConflictIgnore
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Ignore )]
        public int ExistingProperty
        {
            get => 42;
        }

        [Introduce( WhenExists = OverrideStrategy.Ignore )]
        public static int ExistingProperty_Static
        {
            get => 42;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public int ExistingProperty
        {
            get => 27;
        }

        public static int ExistingProperty_Static
        {
            get => 27;
        }
    }
}