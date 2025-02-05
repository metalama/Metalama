using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.ExistingConflictNew_Static
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
        public static int BaseClassMethod()
        {
            meta.InsertComment( "New keyword, call the base class method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassInaccessibleMethod()
        {
            meta.InsertComment( "No new keyword, return a default value." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static int BaseClassMethodHiddenByMethod()
        {
            meta.InsertComment( "New keyword, call the derived class method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static int BaseClassMethodHiddenByInaccessibleMethod()
        {
            meta.InsertComment( "New keyword, call the base class method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static int DerivedClassMethod()
        {
            meta.InsertComment( "New keyword, call the derived class method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static int DerivedClassInaccessibleMethod()
        {
            meta.InsertComment( "No new keyword, return a default value." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static int NonExistentMethod()
        {
            meta.InsertComment( "No new keyword, return a default value." );

            return meta.Proceed();
        }
    }

    internal abstract class BaseClass
    {
        public static int BaseClassMethod()
        {
            return 42;
        }

        private static int BaseClassInaccessibleMethod()
        {
            return 42;
        }

        public static int BaseClassMethodHiddenByMethod()
        {
            return 42;
        }

        public static int BaseClassMethodHiddenByInaccessibleMethod()
        {
            return 42;
        }
    }

    internal class DerivedClass : BaseClass
    {
        public new static int BaseClassMethodHiddenByMethod()
        {
            return 33;
        }

        private new static int BaseClassMethodHiddenByInaccessibleMethod()
        {
            return 33;
        }

        public static int DerivedClassMethod()
        {
            return 33;
        }

        private static int DerivedClassInaccessibleMethod()
        {
            return 33;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass : DerivedClass
    {
        // All methods in this class should contain a comment describing the correct output.
    }
}