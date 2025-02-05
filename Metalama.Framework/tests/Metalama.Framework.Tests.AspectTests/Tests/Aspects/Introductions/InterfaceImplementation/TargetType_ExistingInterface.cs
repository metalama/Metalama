using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.TargetType_ExistingInterface
{
    /*
     * Tests that target implementing another interface does not interfere with interface introduction.
     */

    public class IntroduceAspectAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IIntroducedInterface) );
        }

        [InterfaceMember]
        public void IntroducedMethod()
        {
            Console.WriteLine( "Introduced interface member." );
        }
    }

    public interface IExistingInterface
    {
        void ExistingMethod();
    }

    public interface IIntroducedInterface
    {
        void IntroducedMethod();
    }

    // <target>
    [IntroduceAspect]
    public class TestClass : IExistingInterface
    {
        public void ExistingMethod()
        {
            Console.WriteLine( "Original interface member." );
        }
    }
}