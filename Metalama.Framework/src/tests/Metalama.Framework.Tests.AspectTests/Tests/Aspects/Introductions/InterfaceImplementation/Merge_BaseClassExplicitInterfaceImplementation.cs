// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Skipped(Case for interface merge conflict resolution, not implemented.)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.BaseClassExplicitInterfaceImplementation
{
    /*
     * When the base class of the target type implements the introduced interface explicitly, an error should be produced, because C# does not allow calling
     * base class' explicit interface implementation.
     */

    public interface IInterface
    {
        int InterfaceMethod();
    }

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( (INamedType)TypeFactory.GetType( typeof(IInterface) ) );
        }

        [Introduce]
        public int InterfaceMethod()
        {
            Console.WriteLine( "This is introduced interface method." );

            return meta.Proceed();
        }
    }

    public class BaseClass : IInterface
    {
        int IInterface.InterfaceMethod()
        {
            Console.WriteLine( "This is original interface method." );

            return 27;
        }
    }

    // <target>
    [Introduction]
    public class TargetClass : BaseClass { }
}