// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.AccessorBasedNoGetter
{
    internal class MyAspect : TypeAspect
    {
        [Template]
        public void Setter( int value )
        {
            Console.WriteLine( "Introduced" );
        }

        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceProperty( "TheProperty", null, nameof(Setter) );
        }
    }

    // <target>
    [MyAspect]
    internal class C { }
}