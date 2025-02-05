using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.InterfaceConflict_Fail
{
    /*
     * Tests that when interface
     */

    public interface IInterface
    {
        int InterfaceMethod();
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder
                .ImplementInterface(
                    typeof(IInterface),
                    whenExists: OverrideStrategy.Fail );
        }

        [InterfaceMember]
        public int InterfaceMethod()
        {
            Console.WriteLine( "This is introduced interface method." );

            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    public class TargetClass : IInterface
    {
        int IInterface.InterfaceMethod()
        {
            Console.WriteLine( "This is the original implementation." );

            return 42;
        }
    }
}