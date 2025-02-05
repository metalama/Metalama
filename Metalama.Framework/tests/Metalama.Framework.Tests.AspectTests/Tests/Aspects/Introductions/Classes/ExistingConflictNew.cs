using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Types.ExistingConflictNew
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect(IAspectBuilder<INamedType> builder)
        {
            builder.IntroduceClass("ExistingType", OverrideStrategy.New);
        }
    }

    internal class BaseClass
    {
        public class ExistingType { }
    }

    // <target>
    [Introduction]
    internal class TargetClass : BaseClass { }
}