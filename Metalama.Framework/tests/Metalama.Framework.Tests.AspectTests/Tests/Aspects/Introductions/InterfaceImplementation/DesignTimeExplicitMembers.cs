// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.DesignTimeExplicitMembers
{
    /*
     * Simple case with implicit interface members.
     */

    public interface IInterface
    {
        int InterfaceMethod();

        event EventHandler EventField;

        event EventHandler Event;

        int AutoProperty { get; set; }

        int Property { get; set; }
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( (INamedType)TypeFactory.GetType( typeof(IInterface) ) );
        }

        [InterfaceMember( IsExplicit = true )]
        public int InterfaceMethod()
        {
            Console.WriteLine( "This is introduced interface member." );

            return meta.Proceed();
        }

        [InterfaceMember( IsExplicit = true )]
        public event EventHandler? EventField;

        [InterfaceMember( IsExplicit = true )]
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

        [InterfaceMember( IsExplicit = true )]
        public int AutoProperty { get; set; }

        [InterfaceMember( IsExplicit = true )]
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
    }

    // <target>
    [Introduction]
    public partial class TargetClass { }
}