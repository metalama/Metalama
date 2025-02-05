#if TEST_OPTIONS
// @LanguageFeature(preview)
#endif
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

// This reproduces #30708.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.LanguageFeature
{
    internal class MyAspect : TypeAspect { }

    // <target>
    [MyAspect]
    internal class C { }
}