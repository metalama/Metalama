using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Types.ExistingConflictFail
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect(IAspectBuilder<INamedType> builder)
        {
            builder.IntroduceClass("ExistingType", OverrideStrategy.Fail);
        }
    }

    internal class BaseClass
    {
        public class ExistingType{ }
    }

    // <target>
    [Introduction]
    internal class TargetClass : BaseClass { }
}