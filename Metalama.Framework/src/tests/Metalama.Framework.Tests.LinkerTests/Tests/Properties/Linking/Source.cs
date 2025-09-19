// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.Source
{
    [PseudoLayerOrder( "A1" )]
    [PseudoLayerOrder( "A2" )]
    [PseudoLayerOrder( "A3" )]
    [PseudoLayerOrder( "A4" )]

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

        public int Bar
        {
            get
            {
                Console.WriteLine( "This is original code." );

                return 42;
            }
            set
            {
                Console.WriteLine( "This is original code." );
            }
        }

        [PseudoOverride( nameof(Bar), "A1" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Bar_A1_Override1
        {
            get
            {
                // Should invoke this.Foo_Source.
                _ = Link( This.Foo.get, Base );

                // Should invoke this.Foo_Source.
                _ = Link( This.Foo.get, Previous );

                // Should invoke this.Foo_Source.
                _ = Link( This.Foo.get, Current );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Final );

                return 42;
            }
            set
            {
                // Should invoke this.Foo_Source.
                Link[This.Foo.set, Base] = value;

                // Should invoke this.Foo_Source.
                Link[This.Foo.set, Previous] = value;

                // Should invoke this.Foo_Source.
                Link[This.Foo.set, Current] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Final] = value;
            }
        }

        [PseudoOverride( nameof(Bar), "A2" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Bar_A2_Override2
        {
            get
            {
                // Should invoke this.Foo_Source.
                _ = Link( This.Foo.get, Base );

                // Should invoke this.Foo_Source.
                _ = Link( This.Foo.get, Previous );

                // Should invoke this.Foo_A2_Override3.
                _ = Link( This.Foo.get, Current );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Final );

                return 42;
            }
            set
            {
                // Should invoke this.Foo_Source.
                Link[This.Foo.set, Base] = value;

                // Should invoke this.Foo_Source.
                Link[This.Foo.set, Previous] = value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.set, Current] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Final] = value;
            }
        }

        [PseudoOverride( nameof(Foo), "A2" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_A2_Override3
        {
            get
            {
                // Should invoke this.Foo_Source.
                _ = Link( This.Foo.get, Base );

                // Should invoke this.Foo_Source.
                _ = Link( This.Foo.get, Previous );

                // Should invoke Foo_A2_Override3.
                _ = Link( This.Foo.get, Current );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Final );

                return 42;
            }
            set
            {
                // Should invoke this.Foo_Source.
                Link[This.Foo.set, Base] = value;

                // Should invoke this.Foo_Source.
                Link[This.Foo.set, Previous] = value;

                // Should invoke Foo_A2_Override3.
                Link[This.Foo.set, Current] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Final] = value;
            }
        }

        [PseudoOverride( nameof(Bar), "A2" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Bar_A2_Override4
        {
            get
            {
                // Should invoke this.Foo_Source.
                _ = Link( This.Foo.get, Base );

                // Should invoke this.Foo_A2_Override3.
                _ = Link( This.Foo.get, Previous );

                // Should invoke this.Foo_A2_Override3.
                _ = Link( This.Foo.get, Current );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Final );

                return 42;
            }
            set
            {
                // Should invoke this.Foo_Source.
                Link[This.Foo.set, Base] = value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.set, Previous] = value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.set, Current] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Final] = value;
            }
        }

        [PseudoOverride( nameof(Bar), "A3" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Bar_A3_Override5
        {
            get
            {
                // Should invoke this.Foo_A2_Override3.
                _ = Link( This.Foo.get, Base );

                // Should invoke this.Foo_A2_Override3.
                _ = Link( This.Foo.get, Previous );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Current );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Final );

                return 42;
            }
            set
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.set, Base] = value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.set, Previous] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Current] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Final] = value;
            }
        }

        [PseudoOverride( nameof(Foo), "A3" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_A3_Override6
        {
            get
            {
                // Should invoke this.Foo_A2_Override3.
                _ = Link( This.Foo.get, Base );

                // Should invoke this.Foo_A2_Override3.
                _ = Link( This.Foo.get, Previous );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Current );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Final );

                return 42;
            }
            set
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.set, Base] = value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.set, Previous] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Current] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Final] = value;
            }
        }

        [PseudoOverride( nameof(Bar), "A3" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Bar_A3_Override7
        {
            get
            {
                // Should invoke this.Foo_A2_Override3.
                _ = Link( This.Foo.get, Base );

                // Should invoke this.Foo_A3_Override6.
                _ = Link( This.Foo.get, Previous );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Current );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Final );

                return 42;
            }
            set
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.set, Base] = value;

                // Should invoke this.Foo_A3_Override6.
                Link[This.Foo.set, Previous] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Current] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Final] = value;
            }
        }

        [PseudoOverride( nameof(Foo), "A3" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_A3_Override8
        {
            get
            {
                // Should invoke this.Foo_A2_Override3.
                _ = Link( This.Foo.get, Base );

                // Should invoke this.Foo_A3_Override6.
                _ = Link( This.Foo.get, Previous );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Current );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Final );

                return 42;
            }
            set
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.set, Base] = value;

                // Should invoke this.Foo_A3_Override6.
                Link[This.Foo.set, Previous] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Current] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Final] = value;
            }
        }

        [PseudoOverride( nameof(Bar), "A3" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Bar_A3_Override9
        {
            get
            {
                // Should invoke this.Foo_A2_Override3.
                _ = Link( This.Foo.get, Base );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Previous );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Current );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Final );

                return 42;
            }
            set
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.set, Base] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Previous] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Current] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Final] = value;
            }
        }

        [PseudoOverride( nameof(Bar), "A4" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Bar_A4_Override10
        {
            get
            {
                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Base );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Previous );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Current );

                // Should invoke this.Foo.
                _ = Link( This.Foo.get, Final );

                return 42;
            }
            set
            {
                // Should invoke this.Foo.
                Link[This.Foo.set, Base] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Previous] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Current] = value;

                // Should invoke this.Foo.
                Link[This.Foo.set, Final] = value;
            }
        }
    }
}