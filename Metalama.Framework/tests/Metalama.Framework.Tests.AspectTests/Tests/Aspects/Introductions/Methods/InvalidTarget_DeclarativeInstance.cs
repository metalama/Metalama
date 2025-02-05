using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.InvalidTarget_DeclarativeInstance
{
    /*
     * Tests cases where method is introduced as instance into a type where instance members are not allowed.
     */

    public class ImplicitlyInstanceIntroductionAttribute : TypeAspect
    {
        [Introduce]
        public int Method_ImplicitlyInstance()
        {
            return meta.Proceed();
        }
    }

    public class ExplicitInstanceIntroductionAttribute : TypeAspect
    {
        [Introduce( Scope = IntroductionScope.Instance )]
        public static int Method_ExplicitlyInstance()
        {
            return meta.Proceed();
        }
    }

    // <target>
    [ImplicitlyInstanceIntroduction]
    [ExplicitInstanceIntroduction]
    internal static class StaticTargetClass { }
}