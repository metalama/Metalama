using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.ExistingConflictNew
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static int BaseClassProperty_Static
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int HiddenBaseClassProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static int HiddenBaseClassProperty_Static
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int HiddenBaseClassVirtualProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int HiddenVirtualBaseClassVirtualProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassVirtualProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassVirtualSealedProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassVirtualOverridenProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassAbstractProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int BaseClassAbstractSealedProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int DerivedClassProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static int DerivedClassProperty_Static
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int DerivedClassVirtualProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int DerivedClassVirtualSealedProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int NonExistentProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static int NonExistentProperty_Static
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }
    }

    internal abstract class BaseClass
    {
        public int BaseClassProperty
        {
            get => 42;
        }

        public static int BaseClassProperty_Static
        {
            get => 42;
        }

        public int HiddenBaseClassProperty
        {
            get => 42;
        }

        public static int HiddenBaseClassProperty_Static
        {
            get => 42;
        }

        public int HiddenBaseClassVirtualProperty
        {
            get => 42;
        }

        public int HiddenVirtualBaseClassVirtualProperty
        {
            get => 42;
        }

        public virtual int BaseClassVirtualProperty
        {
            get => 42;
        }

        public virtual int BaseClassVirtualSealedProperty
        {
            get => 42;
        }

        public virtual int BaseClassVirtualOverridenProperty
        {
            get => 42;
        }

        public abstract int BaseClassAbstractProperty { get; }

        public abstract int BaseClassAbstractSealedProperty { get; }
    }

    internal class DerivedClass : BaseClass
    {
        public new int HiddenBaseClassProperty
        {
            get => 33;
        }

        public new static int HiddenBaseClassProperty_Static
        {
            get => 33;
        }

        public new int HiddenBaseClassVirtualProperty
        {
            get => 33;
        }

        public new virtual int HiddenVirtualBaseClassVirtualProperty
        {
            get => 33;
        }

        public sealed override int BaseClassVirtualSealedProperty
        {
            get => 33;
        }

        public override int BaseClassVirtualOverridenProperty
        {
            get => 33;
        }

        public override int BaseClassAbstractProperty
        {
            get => 33;
        }

        public sealed override int BaseClassAbstractSealedProperty
        {
            get => 33;
        }

        public int DerivedClassProperty
        {
            get => 33;
        }

        public static int DerivedClassProperty_Static
        {
            get => 33;
        }

        public virtual int DerivedClassVirtualProperty
        {
            get => 33;
        }

        public virtual int DerivedClassVirtualSealedProperty
        {
            get => 33;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass : DerivedClass { }
}