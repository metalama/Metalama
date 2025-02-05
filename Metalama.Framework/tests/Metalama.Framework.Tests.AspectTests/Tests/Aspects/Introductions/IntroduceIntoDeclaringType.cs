using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Introductions.IntroduceIntoDeclaringType
{
    internal class Aspect : MethodAspect
    {
        [Introduce]
        private void NewMethod() { }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private void M() { }
    }
}