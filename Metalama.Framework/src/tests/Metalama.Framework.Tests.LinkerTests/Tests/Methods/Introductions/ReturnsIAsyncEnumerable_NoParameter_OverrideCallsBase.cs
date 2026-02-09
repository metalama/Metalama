// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0219 // Variable is assigned but its value is never used
#pragma warning disable CS1998 // Async method lacks 'await' operators

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Introductions.ReturnsIAsyncEnumerable_NoParameter_OverrideCallsBase
{
    [PseudoLayerOrder( "A0" )]
    [PseudoLayerOrder( "A1" )]

    // <target>
    internal class Target
    {
        [PseudoIntroduction( "A0" )]
        [PseudoNotInlineable]
        public async IAsyncEnumerable<int> Foo()
        {
            yield return 42;
        }

        [PseudoOverride( nameof(Foo), "A1" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        private async IAsyncEnumerable<int> Foo_Override()
        {
            Console.WriteLine( "Before" );

            // Should invoke the empty method (async enumerables still need _Empty stub).
            IAsyncEnumerable<int> baseResult = Link( This.Foo, Base )();

            await foreach ( var item in baseResult )
            {
                yield return item;
            }

            Console.WriteLine( "After" );
        }
    }
}
