using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Fields.ExistingConflictNew_Error
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
        public int ExistingField;
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public int ExistingField;
    }
}