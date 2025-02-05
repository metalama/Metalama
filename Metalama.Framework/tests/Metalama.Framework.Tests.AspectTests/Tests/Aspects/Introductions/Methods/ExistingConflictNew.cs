using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.ExistingConflictNew
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassMethod()
        {
            meta.InsertComment( "New keyword, call the base method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassInaccessibleMethod()
        {
            meta.InsertComment( "No new keyword, return a default value." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassVirtualMethod()
        {
            meta.InsertComment( "New keyword, call the base method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassVirtualSealedMethod()
        {
            meta.InsertComment( "New keyword, call the base method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassVirtualOverriddenMethod()
        {
            meta.InsertComment( "New keyword, call the base method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassAbstractMethod()
        {
            meta.InsertComment( "New keyword, call the base method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassAbstractSealedMethod()
        {
            meta.InsertComment( "New keyword, call the base method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassMethodHiddenByMethod()
        {
            meta.InsertComment( "New keyword, call the base method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassVirtualMethodHiddenByMethod()
        {
            meta.InsertComment( "New keyword, call the base method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassMethodHiddenByVirtualMethod()
        {
            meta.InsertComment( "New keyword, call the base method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassMethodHiddenByInaccessibleMethod()
        {
            meta.InsertComment( "New keyword, call the base method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int DerivedClassMethod()
        {
            meta.InsertComment( "New keyword, call the base method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int DerivedClassVirtualMethod()
        {
            meta.InsertComment( "New keyword, call the base method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int DerivedClassVirtualSealedMethod()
        {
            meta.InsertComment( "New keyword, call the base method of the same name." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int DerivedClassInaccessibleMethod()
        {
            meta.InsertComment( "No new keyword, return a default value." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int NonExistentMethod()
        {
            meta.InsertComment( "No new keyword, return a default value." );

            return meta.Proceed();
        }
    }

    internal abstract class BaseClass
    {
        public int BaseClassMethod()
        {
            return 42;
        }

        private int BaseClassInaccessibleMethod()
        {
            return 42;
        }

        public virtual int BaseClassVirtualMethod()
        {
            return 42;
        }

        public virtual int BaseClassVirtualSealedMethod()
        {
            return 42;
        }

        public virtual int BaseClassVirtualOverriddenMethod()
        {
            return 42;
        }

        public abstract int BaseClassAbstractMethod();

        public abstract int BaseClassAbstractSealedMethod();

        public int BaseClassMethodHiddenByMethod()
        {
            return 42;
        }

        public int BaseClassVirtualMethodHiddenByMethod()
        {
            return 42;
        }

        public int BaseClassMethodHiddenByVirtualMethod()
        {
            return 42;
        }

        public int BaseClassMethodHiddenByInaccessibleMethod()
        {
            return 42;
        }
    }

    internal class DerivedClass : BaseClass
    {
        public new int BaseClassMethodHiddenByMethod()
        {
            return 33;
        }

        public new int BaseClassVirtualMethodHiddenByMethod()
        {
            return 33;
        }

        public new virtual int BaseClassMethodHiddenByVirtualMethod()
        {
            return 33;
        }

        private new int BaseClassMethodHiddenByInaccessibleMethod()
        {
            return 42;
        }

        public sealed override int BaseClassVirtualSealedMethod()
        {
            return 33;
        }

        public override int BaseClassVirtualOverriddenMethod()
        {
            return 33;
        }

        public override int BaseClassAbstractMethod()
        {
            return 33;
        }

        public sealed override int BaseClassAbstractSealedMethod()
        {
            return 33;
        }

        public int DerivedClassMethod()
        {
            return 33;
        }

        public virtual int DerivedClassVirtualMethod()
        {
            return 33;
        }

        public virtual int DerivedClassVirtualSealedMethod()
        {
            return 33;
        }

        private int DerivedClassInaccessibleMethod()
        {
            return 42;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass : DerivedClass
    {
        // All methods in this class should contain a comment describing the correct output.
    }
}