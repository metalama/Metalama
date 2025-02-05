using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.InterfaceConflict_DerivedAfterBase_Fail
{
    /*
     * Tests that when a single aspect introduces a base interface before the derived interface, the remainder of derived interface is implemented.
     */

    public interface IBaseInterface
    {
        int Foo();
    }

    public interface IDerivedInterface : IBaseInterface
    {
        int Bar();
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IBaseInterface) );
            aspectBuilder.ImplementInterface( typeof(IDerivedInterface), OverrideStrategy.Fail );
        }

        [InterfaceMember]
        public int Foo()
        {
            return meta.Proceed();
        }

        [InterfaceMember]
        public int Bar()
        {
            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}