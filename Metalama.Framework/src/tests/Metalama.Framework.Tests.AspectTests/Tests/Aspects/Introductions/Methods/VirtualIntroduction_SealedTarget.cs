// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.VirtualIntroduction_SealedTarget
{
    /*
     * Tests that IsVirtual is ignored when introducing a method into a sealed target type.
     * The method should be introduced as non-virtual instead of producing an error.
     */

    public class ProgrammaticVirtualIntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod( nameof(ImplicitlyVirtualMethod) );
            builder.IntroduceMethod( nameof(ExplicitlyVirtualMethod), buildMethod: b => b.IsVirtual = true );
        }

        [Template]
        public virtual int ImplicitlyVirtualMethod()
        {
            Console.WriteLine( "Introduced." );

            return 42;
        }

        [Template]
        public int ExplicitlyVirtualMethod()
        {
            Console.WriteLine( "Introduced." );

            return 42;
        }
    }

    public class DeclarativeVirtualIntroductionAttribute : TypeAspect
    {
        [Introduce]
        public virtual int DeclarativeImplicitlyVirtualMethod()
        {
            Console.WriteLine( "Introduced." );

            return 42;
        }

        [Introduce( IsVirtual = true )]
        public int DeclarativeExplicitlyVirtualMethod()
        {
            Console.WriteLine( "Introduced." );

            return 42;
        }
    }

    // <target>
    [ProgrammaticVirtualIntroduction]
    [DeclarativeVirtualIntroduction]
    internal sealed class SealedTargetClass { }
}
