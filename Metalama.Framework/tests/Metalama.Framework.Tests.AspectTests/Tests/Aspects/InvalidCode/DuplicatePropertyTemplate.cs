#if TEST_OPTIONS
// @AcceptInvalidInput
#endif

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.InvalidCode.DuplicatePropertyTemplate;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

class Aspect : TypeAspect
{
    [Template]
    public object Instance { get; set; }

#if TESTRUNNER
    [Template]
    public object Instance { get; set; }
#endif
}