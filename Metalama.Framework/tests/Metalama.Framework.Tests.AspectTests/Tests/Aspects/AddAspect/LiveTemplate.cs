using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.LiveTemplate
{
    [EditorExperience( SuggestAsLiveTemplate = true )]
    internal class Aspect : MethodAspect { }

    // <target>
    internal class T { }
}