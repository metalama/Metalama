using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.ExistingConflictNew_Static_Error
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
        public static int ExistingMethod()
        {
            meta.InsertComment( "No new keyword, return a constant." );

            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        // All methods in this class should contain a comment describing the correct output.

        public static int ExistingMethod()
        {
            return 27;
        }
    }
}