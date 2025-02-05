using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.DeclarativeRunTimeOnly
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public void IntroducedMethodWithParam( RunTimeOnlyClass p ) { }

        [Introduce]
        public RunTimeOnlyClass? IntroducedMethodWithRet()
        {
            return null;
        }
    }

    public class RunTimeOnlyClass { }

    // <target>
    [Introduction]
    internal class TargetClass { }
}