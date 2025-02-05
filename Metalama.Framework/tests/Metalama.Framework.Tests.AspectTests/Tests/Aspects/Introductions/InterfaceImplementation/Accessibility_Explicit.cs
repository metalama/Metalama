using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Accessibility_Explicit
{
    /*
     * Tests accessibility of explicit members (should be always ignored).
     */

    public interface IInterface
    {
        void Method();

        int Property { get; set; }

        int Property_GetOnly { get; }

        int Property_ExpressionBody { get; }

        int AutoProperty { get; set; }

        event EventHandler? EventField;

        event EventHandler? Event;
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [InterfaceMember( IsExplicit = true )]
        private void Method()
        {
            Console.WriteLine( "Introduced interface member" );
        }

        [InterfaceMember( IsExplicit = true )]
        private int Property
        {
            get
            {
                return 42;
            }

            set { }
        }

        [InterfaceMember( IsExplicit = true )]
        private int Property_GetOnly
        {
            get
            {
                return 42;
            }
        }

        [InterfaceMember( IsExplicit = true )]
        private int Property_ExpressionBody => 42;

        [InterfaceMember( IsExplicit = true )]
        private int AutoProperty { get; set; }

        [InterfaceMember( IsExplicit = true )]
        private event EventHandler? EventField;

        [InterfaceMember( IsExplicit = true )]
        private event EventHandler? Event
        {
            add { }
            remove { }
        }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}