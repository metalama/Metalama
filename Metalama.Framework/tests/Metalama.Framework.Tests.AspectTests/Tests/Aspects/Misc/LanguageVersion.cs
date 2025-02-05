#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
// @LanguageVersion(8.0)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

// This reproduces #30669.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.LanguageVersion
{
    internal class MyAspect : TypeAspect { }

    // <target>
    [MyAspect]
    internal class C { }
}