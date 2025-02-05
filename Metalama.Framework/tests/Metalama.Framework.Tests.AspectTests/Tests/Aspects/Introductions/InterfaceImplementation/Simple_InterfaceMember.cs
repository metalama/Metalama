using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Simple_InterfaceMember
{
    /*
     * Tests a simple case with implicit interface member.
     */

    public interface IInterface
    {
        void Foo();
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [Introduce]
        public void Foo()
        {
            Console.WriteLine( "Introduced interface member" );
        }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}