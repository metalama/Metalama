#if TEST_OPTIONS
// @LanguageVersion(preview)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LanguageVersion.LanguageVersionPreview;

public class TheAspect : TypeAspect
{
    [Introduce]
    public string Field;
}

// <target>
[TheAspect]
internal class Target { }