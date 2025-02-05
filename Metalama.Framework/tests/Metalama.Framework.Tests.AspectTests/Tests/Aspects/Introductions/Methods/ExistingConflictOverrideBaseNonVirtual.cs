using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.ExistingConflictOverrideBaseNonVirtual
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int BaseMethod()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public static int BaseMethod_Static()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }
    }

    internal class BaseClass
    {
        public int BaseMethod()
        {
            return 13;
        }

        public static int BaseMethod_Static()
        {
            return 13;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass : BaseClass { }
}