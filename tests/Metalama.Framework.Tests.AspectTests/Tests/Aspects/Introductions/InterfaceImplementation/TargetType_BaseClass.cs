using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.TargetType_BaseClass
{
    /*
     * Tests that target having a base class does not interfere with interface introduction.
     */

    public class IntroduceAspectAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [InterfaceMember]
        public void IntroducedMethod()
        {
            Console.WriteLine( "Introduced interface member" );
        }
    }

    public interface IInterface
    {
        void IntroducedMethod();
    }

    public abstract class BaseClass
    {
        public abstract void ExistingMethod();
    }

    // <target>
    [IntroduceAspect]
    public class TargetClass : BaseClass
    {
        public override void ExistingMethod()
        {
            Console.WriteLine( "Original interface member" );
        }
    }
}