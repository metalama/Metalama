using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.DuplicateMemberName2
{
    // Error: the base class already defines a template and this is not an override.

    internal class Aspect1 : MethodAspect
    {
        [Template]
        public void Template() { }
    }

    internal class Aspect2 : Aspect1
    {
        [Template]
        public new void Template() { }
    }

    // <target>
    internal class Target { }
}