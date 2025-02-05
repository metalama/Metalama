using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ImplicitMembers
{
    /*
     * Simple case with implicit interface members.
     */

    public interface IInterface
    {
        int InterfaceMethod();

        event EventHandler Event;

        event EventHandler? EventField;

        int Property { get; set; }

        int Property_PrivateSet { get; }

        int AutoProperty { get; set; }

        int AutoProperty_PrivateSet { get; }
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [InterfaceMember]
        public int InterfaceMethod()
        {
            Console.WriteLine( "This is introduced interface member." );

            return meta.Proceed();
        }

        [InterfaceMember]
        public event EventHandler? Event
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
        public event EventHandler? EventField;

        [InterfaceMember]
        public int Property
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

        [InterfaceMember]
        public int Property_PrivateSet
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

        [InterfaceMember]
        public int AutoProperty { get; set; }

        [InterfaceMember]
        public int AutoProperty_PrivateSet { get; private set; }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}