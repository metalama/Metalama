using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.ExistingConflictNew_Error
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
        public int ExistingMethod()
        {
            meta.InsertComment( "No new keyword, return a constant." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int ExistingVirtualMethod()
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

        public int ExistingMethod()
        {
            return 27;
        }

        public virtual int ExistingVirtualMethod()
        {
            return 27;
        }
    }
}