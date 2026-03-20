// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.VirtualIntroduction_SealedTarget
{
    /*
     * Tests that IsVirtual is ignored when introducing a property into a sealed target type.
     * The property should be introduced as non-virtual instead of producing an error.
     */

    public class ProgrammaticVirtualIntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceProperty( nameof(ImplicitlyVirtualProperty) );
            builder.IntroduceProperty( nameof(ExplicitlyVirtualProperty), buildProperty: b => b.IsVirtual = true );
        }

        [Template]
        public virtual int ImplicitlyVirtualProperty
        {
            get
            {
                Console.WriteLine( "Introduced." );

                return 42;
            }

            set
            {
                Console.WriteLine( "Introduced." );
            }
        }

        [Template]
        public int ExplicitlyVirtualProperty
        {
            get
            {
                Console.WriteLine( "Introduced." );

                return 42;
            }

            set
            {
                Console.WriteLine( "Introduced." );
            }
        }
    }

    public class DeclarativeVirtualIntroductionAttribute : TypeAspect
    {
        [Introduce]
        public virtual int DeclarativeImplicitlyVirtualProperty
        {
            get
            {
                Console.WriteLine( "Introduced." );

                return 42;
            }

            set
            {
                Console.WriteLine( "Introduced." );
            }
        }

        [Introduce( IsVirtual = true )]
        public int DeclarativeExplicitlyVirtualProperty
        {
            get
            {
                Console.WriteLine( "Introduced." );

                return 42;
            }

            set
            {
                Console.WriteLine( "Introduced." );
            }
        }
    }

    // <target>
    [ProgrammaticVirtualIntroduction]
    [DeclarativeVirtualIntroduction]
    internal sealed class SealedTargetClass { }

    // <target>
    [ProgrammaticVirtualIntroduction]
    [DeclarativeVirtualIntroduction]
    internal struct TargetStruct { }
}
