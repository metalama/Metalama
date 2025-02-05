using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.ExistingConflictNew_Error
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
        public int ExistingProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static int ExistingProperty_Static
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int ExistingVirtualProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public int ExistingProperty
        {
            get => 27;
        }

        public static int ExistingProperty_Static
        {
            get => 27;
        }

        public virtual int ExistingVirtualProperty
        {
            get => 27;
        }
    }
}