#if TEST_OPTIONS
// @RemoveOutputCode
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32523;

public class TheAspect : TypeAspect
{
    public string[] InitOnlyProperty { get; init; } = null!;
}

[CompileTime]
public static class CompileTimeClass
{
    public static void Method()
    {
        _ = new TheAspect() { InitOnlyProperty = new[] { "" } };
    }
}