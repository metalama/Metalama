using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.ExistingConflictOverrideDifferentReturnType
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int ExistingProperty
        {
            get => meta.Proceed();
        }
    }

    internal class BaseClass
    {
        public virtual object? ExistingProperty
        {
            get => default;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass : BaseClass { }
}