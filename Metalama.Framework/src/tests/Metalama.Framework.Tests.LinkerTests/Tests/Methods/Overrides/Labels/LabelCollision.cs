// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// The target body and the override body declare a label of the same name. The inlining splices the target body into
// the statement list of the override, so the two declarations reach one scope. The linker has to rename the label of
// the inlined body, otherwise the transformed compilation reports CS0140 or CS0158. See issue #1947.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Labels.LabelCollision
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

        myLabel:
            Link( This.Foo, Inline )( x );

            Console.WriteLine( "After aspect" );
        }
    }
}
