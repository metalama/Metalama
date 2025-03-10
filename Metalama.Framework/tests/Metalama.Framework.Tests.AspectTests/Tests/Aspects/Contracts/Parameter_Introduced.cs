// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Parameter_Introduced
{
    // Tests that a contract on introduced ctor parameter work properly.

    internal class TestAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var constructor in builder.Target.Constructors)
            {
                var parameter = builder.With( constructor )
                    .IntroduceParameter(
                        "dependency",
                        typeof(int),
                        TypedConstant.Create( 0 ) )
                    .Declaration;

                builder.With( parameter ).AddContract( nameof(Validate) );
            }
        }

        [Template]
        public void Validate( int value )
        {
            if (value == 0)
            {
                throw new ArgumentOutOfRangeException( meta.Target.Parameter.Name );
            }
        }
    }

    // <target>
    [Test]
    internal class Target
    {
        public Target() { }

        public Target( int x ) { }
    }
}