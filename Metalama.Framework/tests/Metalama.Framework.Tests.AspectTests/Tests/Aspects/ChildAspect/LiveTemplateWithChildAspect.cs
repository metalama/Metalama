#if TEST_OPTIONS
// @TestScenario(LiveTemplate)
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Code;
using Metalama.Testing.AspectTesting;

namespace Metalama.Framework.IntegrationTests.LiveTemplates.LiveTemplateWithChildAspect;

class CommonAspect : TypeAspect
{
    [Introduce]
    int i;
}

[EditorExperience(SuggestAsLiveTemplate = true)]
[Inheritable]
internal class LiveAspect : TypeAspect
{
    [Introduce]
    int j;

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.Outbound.AddAspect<CommonAspect>();
    }
}

// <target>
[TestLiveTemplate(typeof(LiveAspect))]
class Target
{
}