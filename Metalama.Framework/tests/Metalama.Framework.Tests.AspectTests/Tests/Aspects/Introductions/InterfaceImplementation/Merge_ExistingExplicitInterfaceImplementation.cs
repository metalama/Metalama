// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Skipped(Case for interface merge conflict resolution, not implemented.)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExistingExplicitInterfaceImplementation
{
    /*
     * When the target class already explicitly implements the introduced interface (or it's subinterface), the explicit implementation should be overridden.
     */

    public interface ISubInterface
    {
        int SubInterfaceMethod();
    }

    public interface ISuperInterface
    {
        int SuperInterfaceMethod();
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( (INamedType)TypeFactory.GetType( typeof(ISuperInterface) ) );
        }

        [Introduce]
        public int SubInterfaceMethod()
        {
            Console.WriteLine( "This is introduced interface method." );

            return meta.Proceed();
        }

        [Introduce]
        public int SuperInterfaceMethod()
        {
            Console.WriteLine( "This is introduced interface method." );

            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    public class TargetClass : ISubInterface
    {
        int ISubInterface.SubInterfaceMethod()
        {
            Console.WriteLine( "This is original interface method." );

            return 27;
        }
    }
}