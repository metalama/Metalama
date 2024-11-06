using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.SharedInterfaceMember_Implicit
{
    /*
     * Tests that a single implicit interface member matching two interfaces results in an error.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface1) );
            aspectBuilder.ImplementInterface( typeof(IInterface2) );
        }

        [InterfaceMember( IsExplicit = false )]
        public void Method()
        {
            Console.WriteLine( "Interface member." );
        }
    }

    public interface IInterface1
    {
        void Method();
    }

    public interface IInterface2
    {
        void Method();
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}