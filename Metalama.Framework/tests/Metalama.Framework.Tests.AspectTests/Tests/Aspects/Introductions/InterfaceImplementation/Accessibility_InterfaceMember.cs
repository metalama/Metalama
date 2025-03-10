// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0067, CS0618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Accessibility_InterfaceMember
{
    /*
     * Tests accessibility of implicit members.
     */

    public interface IInterface
    {
        void Method();

        int Property { get; set; }

        int Property_PrivateSetter { get; }

        int Property_GetOnly { get; }

        int Property_ExpressionBody { get; }

        int AutoProperty { get; set; }

        int AutoProperty_PrivateSetter { get; set; }

        event EventHandler? EventField;

        event EventHandler? Event;
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [InterfaceMember( IsExplicit = false )]
        private void Method()
        {
            Console.WriteLine( "Introduced interface member" );
        }

        [InterfaceMember( IsExplicit = false )]
        private int Property
        {
            get
            {
                return 42;
            }

            set { }
        }

        [InterfaceMember( IsExplicit = false )]
        public int Property_PrivateSetter
        {
            get
            {
                return 42;
            }

            private set { }
        }

        [InterfaceMember( IsExplicit = false )]
        private int Property_GetOnly
        {
            get
            {
                return 42;
            }
        }

        [InterfaceMember( IsExplicit = false )]
        private int Property_ExpressionBody => 42;

        [InterfaceMember( IsExplicit = false )]
        private int AutoProperty { get; set; }

        [InterfaceMember( IsExplicit = false )]
        public int AutoProperty_PrivateSetter { get; private set; }

        [InterfaceMember( IsExplicit = false )]
        private event EventHandler? EventField;

        [InterfaceMember( IsExplicit = false )]
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