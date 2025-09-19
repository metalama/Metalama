// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Fields.Linking.Introduced_NoPromotion
{
    [PseudoLayerOrder( "A0" )]
    [PseudoLayerOrder( "A1" )]
    [PseudoLayerOrder( "A2" )]

    // <target>
    internal class Target
    {
        public int Foo
        {
            get
            {
                Console.WriteLine( "This is original code (discarded)." );

                return 42;
            }
            set
            {
                Console.WriteLine( "This is original code (discarded)." );
            }
        }

        [PseudoIntroduction( "A1" )]
        [PseudoNotInlineable]
        public int Bar;

        [PseudoOverride( nameof(Foo), "A0" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_Override0
        {
            get
            {
                // Should invoke empty code.
                _ = Link( This.Bar, Base );

                // Should invoke empty code.
                _ = Link( This.Bar, Previous );

                // Should invoke empty code.
                _ = Link( This.Bar, Current );

                // Should invoke the final declaration.
                _ = Link( This.Bar, Final );

                return 42;
            }
            set
            {
                // Should invoke empty code.
                Link[This.Bar, Base] = value;

                // Should invoke empty code.
                Link[This.Bar, Previous] = value;

                // Should invoke empty code.
                Link[This.Bar, Current] = value;

                // Should invoke the final declaration.
                Link[This.Bar, Final] = value;
            }
        }

        [PseudoOverride( nameof(Foo), "A1" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_Override1
        {
            get
            {
                // Should invoke empty code.
                _ = Link( This.Bar, Base );

                // Should invoke the final declaration.
                _ = Link( This.Bar, Previous );

                // Should invoke the final declaration.
                _ = Link( This.Bar, Current );

                // Should invoke the final declaration.
                _ = Link( This.Bar, Final );

                return 42;
            }
            set
            {
                // Should invoke empty code.
                Link[This.Bar, Base] = value;

                // Should invoke the final declaration.
                Link[This.Bar, Previous] = value;

                // Should invoke the final declaration.
                Link[This.Bar, Current] = value;

                // Should invoke the final declaration.
                Link[This.Bar, Final] = value;
            }
        }

        [PseudoOverride( nameof(Foo), "A2" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_Override2
        {
            get
            {
                // Should invoke the final declaration.
                _ = Link( This.Bar, Base );

                // Should invoke the final declaration.
                _ = Link( This.Bar, Previous );

                // Should invoke the final declaration.
                _ = Link( This.Bar, Current );

                // Should invoke the final declaration.
                _ = Link( This.Bar, Final );

                return 42;
            }
            set
            {
                // Should invoke the final declaration.
                Link[This.Bar, Base] = value;

                // Should invoke the final declaration.
                Link[This.Bar, Previous] = value;

                // Should invoke the final declaration.
                Link[This.Bar, Current] = value;

                // Should invoke the final declaration.
                Link[This.Bar, Final] = value;
            }
        }
    }
}