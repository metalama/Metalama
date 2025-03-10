// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.PublicPipeline.Aspects.Inheritance.IntroducedDerivedType;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(IntroduceClassAspect), typeof(IntroduceMethodInheritableAspect))]
namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Inheritance.IntroducedDerivedType
{
    [Inheritable]
    internal class IntroduceMethodInheritableAspect : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override, IsVirtual = true )]
        public int Foo()
        {
            Console.WriteLine( "Introduced!" );

            return meta.Proceed();
        }
    }

    internal class IntroduceClassAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceClass(
                "IntroducedDerived",
                buildType: b => { b.BaseType = builder.Target.Types.OfName( "BaseType" ).Single(); } );
        }
    }

    // <target>
    [IntroduceClassAspect]
    public class Targets
    {
        [IntroduceMethodInheritableAspect]
        public class BaseType { }

        public class ManualDerived : BaseType { }
    }
}