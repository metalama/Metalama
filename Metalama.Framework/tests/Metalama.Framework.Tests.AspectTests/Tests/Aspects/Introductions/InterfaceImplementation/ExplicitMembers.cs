using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers
{
    /*
     * Simple case with explicit interface members for a single interface.
     */

    public interface IInterface
    {
        int InterfaceMethod();

        event EventHandler Event;

        event EventHandler? EventField;

        int Property { get; set; }

        int AutoProperty { get; set; }
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [InterfaceMember( IsExplicit = true )]
        public int InterfaceMethod()
        {
            Console.WriteLine( "This is introduced interface member." );

            return meta.Proceed();
        }

        [InterfaceMember( IsExplicit = true )]
        public event EventHandler? Event
        {
            add
            {
                Console.WriteLine( "This is introduced interface member." );
                meta.Proceed();
            }

            remove
            {
                Console.WriteLine( "This is introduced interface member." );
                meta.Proceed();
            }
        }

        [InterfaceMember( IsExplicit = true )]
        public event EventHandler? EventField;

        [InterfaceMember( IsExplicit = true )]
        public int Property
        {
            get
            {
                Console.WriteLine( "This is introduced interface member." );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "This is introduced interface member." );
                meta.Proceed();
            }
        }

        [InterfaceMember( IsExplicit = true )]
        public int AutoProperty { get; set; }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}