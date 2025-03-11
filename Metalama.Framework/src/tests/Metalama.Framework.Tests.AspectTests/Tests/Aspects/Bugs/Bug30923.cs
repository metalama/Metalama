// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30923
{
    public sealed class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            base.BuildAspect( builder );

            foreach (var property in builder.Target.Properties.Where( p => p.Type.IsConvertibleTo( typeof(int) ) ))
            {
                builder.With( property ).Override( nameof(TheAnswer) );
            }
        }

        [Template]
        public int TheAnswer => 42;
    }

    // <target>
    [TestAspect]
    public readonly struct TestStruct
    {
        public TestStruct()
        {
            Test = 0;
        }

        public TestStruct( int test )
        {
            Test = test;
        }

        public int Test { get; }
    }
}