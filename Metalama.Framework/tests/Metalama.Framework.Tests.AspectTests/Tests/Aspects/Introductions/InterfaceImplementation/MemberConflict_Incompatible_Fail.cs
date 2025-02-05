using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.MemberConflict_Incompatible_Fail
{
    /*
     * Tests that when a incompatible member of the same signature already exists and whenExists is set to Fail, errors are produced.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Fail )]
        public int Method()
        {
            Console.WriteLine( "This is introduced interface method." );

            return 42;
        }

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Fail )]
        public int Property
        {
            get
            {
                Console.WriteLine( "This is introduced interface property." );

                return 42;
            }

            set
            {
                Console.WriteLine( "This is introduced interface property." );
            }
        }

        [InterfaceMember( WhenExists = InterfaceMemberOverrideStrategy.Fail )]
        public event EventHandler Event
        {
            add
            {
                Console.WriteLine( "This is introduced interface event." );
            }

            remove
            {
                Console.WriteLine( "This is introduced interface event." );
            }
        }
    }

    public interface IInterface
    {
        int Method();

        int Property { get; set; }

        event EventHandler Event;
    }

    // <target>
    [Introduction]
    public class TargetClass
    {
        public string Method()
        {
            Console.WriteLine( "This is original method." );

            return "42";
        }

        public string Property
        {
            get
            {
                Console.WriteLine( "This is original property." );

                return "42";
            }

            set
            {
                Console.WriteLine( "This is original property." );
            }
        }

        public event Action Event
        {
            add
            {
                Console.WriteLine( "This is original event." );
            }

            remove
            {
                Console.WriteLine( "This is original event." );
            }
        }
    }
}