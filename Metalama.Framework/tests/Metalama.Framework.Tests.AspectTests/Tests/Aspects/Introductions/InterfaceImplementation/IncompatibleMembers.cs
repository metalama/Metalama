using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.IncompatibleMembers
{
    /*
     * Tests that when aspect members marked with [InterfaceMember] are not compatible with interface members of the same signature
     */

    public interface IInterface
    {
        int Method();

        event EventHandler Event;

        int Property { get; set; }
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [InterfaceMember]
        public long Method()
        {
            Console.WriteLine( "This is introduced interface member." );

            return meta.Proceed();
        }

        [InterfaceMember]
        public event UnhandledExceptionEventHandler? Event
        {
            add
            {
                Console.WriteLine( "This is introduced interface member." );
            }

            remove
            {
                Console.WriteLine( "This is introduced interface member." );
            }
        }

        [InterfaceMember]
        public long Property
        {
            get
            {
                Console.WriteLine( "This is introduced interface member." );

                return 42;
            }

            set
            {
                Console.WriteLine( "This is introduced interface member." );
            }
        }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}