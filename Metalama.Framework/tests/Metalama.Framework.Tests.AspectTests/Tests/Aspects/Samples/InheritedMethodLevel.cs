#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
# endif



/*
 * Doc sample. Bug #30453.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Samples.InheritedMethodLevel
{
#if NET5_0_OR_GREATER
    // <target>
    internal class BaseClass
    {
        [InheritedAspect]
        public virtual void ClassMethodWithAspect() { }

        public virtual void ClassMethodWithoutAspect() { }
    }
    
    // <target>
    internal interface IInterface
    {
        [InheritedAspect]
        private void InterfaceMethodWithAspect() { }

        private void InterfaceMethodWithoutAspect() { }
    }
    
    // <target>
    internal class DerivedClass : BaseClass, IInterface
    {
        public override void ClassMethodWithAspect()
        {
            base.ClassMethodWithAspect();
        }

        public override void ClassMethodWithoutAspect()
        {
            base.ClassMethodWithoutAspect();
        }

        public virtual void InterfaceMethodWithAspect() { }
        public virtual void InterfaceMethodWithoutAspect() { }

    }
    
    // <target>
    internal class DerivedTwiceClass : DerivedClass
    {
        public override void ClassMethodWithAspect()
        {
            base.ClassMethodWithAspect();
        }

        public override void ClassMethodWithoutAspect()
        {
            base.ClassMethodWithoutAspect();
        }

        public override void InterfaceMethodWithAspect()
        {
            base.InterfaceMethodWithAspect();
        }

        public override void InterfaceMethodWithoutAspect()
        {
            base.InterfaceMethodWithoutAspect();
        }
    }
#endif
}