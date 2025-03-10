// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.InterfaceConflict_BaseAfterDerived_Ignore
{
    /*
     * Tests that when a single aspect introduces a base interface after the derived interface and whenExists is Ignore, the interface is ignored.
     */

    public interface IBaseInterface
    {
        int Foo();
    }

    public interface IDerivedInterface : IBaseInterface
    {
        int Bar();
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IDerivedInterface), tags: new { Source = "Derived" } );
            aspectBuilder.ImplementInterface( typeof(IBaseInterface), OverrideStrategy.Ignore, tags: new { Source = "Base" } );
        }

        [InterfaceMember]
        public int Foo()
        {
            Console.WriteLine( $"This is introduced interface member by {meta.Tags["Source"]} (should be Derived)." );

            return meta.Proceed();
        }

        [InterfaceMember]
        public int Bar()
        {
            Console.WriteLine( $"This is introduced interface member by {meta.Tags["Source"]} (should be Derived)." );

            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    public class TargetClass { }
}