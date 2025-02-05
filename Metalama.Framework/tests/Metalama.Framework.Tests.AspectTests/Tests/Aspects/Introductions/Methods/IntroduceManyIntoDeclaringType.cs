using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Introductions.Methods.IntroduceManyIntoDeclaringType
{
    internal class Aspect : MethodAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Ignore )]
        private void NewMethod() { }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private void M() { }

        [Aspect]
        private void M2() { }
    }
}