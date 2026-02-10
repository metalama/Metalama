// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0219 // Variable is assigned but its value is never used

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Introductions.ReturnsVoid_NoParameter_OverrideCallsBase
{
    [PseudoLayerOrder( "A0" )]
    [PseudoLayerOrder( "A1" )]

    // <target>
    internal class Target
    {
        [PseudoIntroduction( "A0" )]
        [PseudoNotInlineable]
        public void Foo()
        {
            Console.WriteLine( "SHOULD BE DISCARDED (this is introduced code)." );
        }

        [PseudoOverride( nameof(Foo), "A1" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        private void Foo_Override()
        {
            Console.WriteLine( "Before" );

            // Should invoke empty code (statement should be suppressed).
            Link( This.Foo, Base )();

            Console.WriteLine( "After" );
        }
    }
}
