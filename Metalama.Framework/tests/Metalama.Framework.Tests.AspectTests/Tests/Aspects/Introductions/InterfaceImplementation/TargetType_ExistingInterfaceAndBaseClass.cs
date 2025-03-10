// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.TargetType_ExistingInterfaceAndBaseClass
{
    /*
     * Tests that target implementing a base class and another interface does not interfere with interface introduction.
     */

    public class IntroduceAspectAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IIntroducedInterface) );
        }

        [InterfaceMember]
        public void IntroducedMethod()
        {
            Console.WriteLine( "Introduced interface member." );
        }
    }

    public interface IExistingInterface
    {
        void ExistingMethod();
    }

    public interface IIntroducedInterface
    {
        void IntroducedMethod();
    }

    public abstract class BaseClass
    {
        public abstract void ExistingBaseMethod();
    }

    // <target>
    [IntroduceAspect]
    public class TestClass : BaseClass, IExistingInterface
    {
        public void ExistingMethod()
        {
            Console.WriteLine( "Original interface member." );
        }

        public override void ExistingBaseMethod()
        {
            Console.WriteLine( "Original base class member." );
        }
    }
}