// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.TargetType_BaseClass
{
    /*
     * Tests that target having a base class does not interfere with interface introduction.
     */

    public class IntroduceAspectAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
        {
            aspectBuilder.ImplementInterface( typeof(IInterface) );
        }

        [InterfaceMember]
        public void IntroducedMethod()
        {
            Console.WriteLine( "Introduced interface member" );
        }
    }

    public interface IInterface
    {
        void IntroducedMethod();
    }

    public abstract class BaseClass
    {
        public abstract void ExistingMethod();
    }

    // <target>
    [IntroduceAspect]
    public class TargetClass : BaseClass
    {
        public override void ExistingMethod()
        {
            Console.WriteLine( "Original interface member" );
        }
    }
}