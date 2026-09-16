// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// The target body and the override body declare a label of the same name, and the inlined body is flattened into the
// statement list of the override instead of staying in a nested block. The two declarations therefore reach the same
// declaration space, which is CS0140. See issue #1947.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Labels.LabelCollision_Flattened
{
    // <target>
    internal class Target
    {
        private void Foo( int x )
        {
            if ( x == 1 )
            {
                goto myLabel;
            }

            Console.WriteLine( "Original start" );

        myLabel:
            Console.WriteLine( "Original end" );
        }

        [PseudoOverride( nameof(Foo), "TestAspect" )]
        private void Foo_Override( int x )
        {
            if ( x == 2 )
            {
                goto myLabel;
            }

            Console.WriteLine( "Before aspect" );

            Link( This.Foo, Inline )( x );

        myLabel:
            Console.WriteLine( "After aspect" );
        }
    }
}
