using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.ExistingConflictFail
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Fail )]
        public int ExistingMethod()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce( WhenExists = OverrideStrategy.Fail )]
        public static int ExistingMethod_Static()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public int ExistingMethod()
        {
            return 13;
        }

        public static int ExistingMethod_Static()
        {
            return 13;
        }
    }
}