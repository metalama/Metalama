// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.Source
{
    [PseudoLayerOrder( "A1" )]
    [PseudoLayerOrder( "A2" )]
    [PseudoLayerOrder( "A3" )]
    [PseudoLayerOrder( "A4" )]

    // <target>
    internal class Target
    {
        public event EventHandler Foo
        {
            add
            {
                Console.WriteLine( "This is original code (discarded)." );
            }
            remove
            {
                Console.WriteLine( "This is original code (discarded)." );
            }
        }

        public event EventHandler Bar
        {
            add
            {
                Console.WriteLine( "This is original code." );
            }
            remove
            {
                Console.WriteLine( "This is original code." );
            }
        }

        [PseudoOverride( nameof(Bar), "A1" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Bar_A1_Override1
        {
            add
            {
                // Should invoke this.Foo_Source.
                Link[This.Foo.add, Base] += value;

                // Should invoke this.Foo_Source.
                Link[This.Foo.add, Previous] += value;

                // Should invoke this.Foo_Source.
                Link[This.Foo.add, Current] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Final] += value;
            }
            remove
            {
                // Should invoke this.Foo_Source.
                Link[This.Foo.remove, Base] -= value;

                // Should invoke this.Foo_Source.
                Link[This.Foo.remove, Previous] -= value;

                // Should invoke this.Foo_Source.
                Link[This.Foo.remove, Current] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "A2" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Bar_A2_Override2
        {
            add
            {
                // Should invoke this.Foo_Source.
                Link[This.Foo.add, Base] += value;

                // Should invoke this.Foo_Source.
                Link[This.Foo.add, Previous] += value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.add, Current] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Final] += value;
            }
            remove
            {
                // Should invoke this.Foo_Source.
                Link[This.Foo.remove, Base] -= value;

                // Should invoke this.Foo_Source.
                Link[This.Foo.remove, Previous] -= value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.remove, Current] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Foo), "A2" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_A2_Override3
        {
            add
            {
                // Should invoke this.Foo_Source.
                Link[This.Foo.add, Base] += value;

                // Should invoke this.Foo_Source.
                Link[This.Foo.add, Previous] += value;

                // Should invoke Foo_A2_Override3.
                Link[This.Foo.add, Current] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Final] += value;
            }
            remove
            {
                // Should invoke this.Foo_Source.
                Link[This.Foo.remove, Base] -= value;

                // Should invoke this.Foo_Source.
                Link[This.Foo.remove, Previous] -= value;

                // Should invoke Foo_A2_Override3.
                Link[This.Foo.remove, Current] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "A2" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Bar_A2_Override4
        {
            add
            {
                // Should invoke this.Foo_Source.
                Link[This.Foo.add, Base] += value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.add, Previous] += value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.add, Current] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Final] += value;
            }
            remove
            {
                // Should invoke this.Foo_Source.
                Link[This.Foo.remove, Base] -= value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.remove, Previous] -= value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.remove, Current] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "A3" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Bar_A3_Override5
        {
            add
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.add, Base] += value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.add, Previous] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Current] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Final] += value;
            }
            remove
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.remove, Base] -= value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.remove, Previous] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Current] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Foo), "A3" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_A3_Override6
        {
            add
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.add, Base] += value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.add, Previous] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Current] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Final] += value;
            }
            remove
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.remove, Base] -= value;

                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.remove, Previous] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Current] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "A3" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Bar_A3_Override7
        {
            add
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.add, Base] += value;

                // Should invoke this.Foo_A3_Override6.
                Link[This.Foo.add, Previous] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Current] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Final] += value;
            }
            remove
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.remove, Base] -= value;

                // Should invoke this.Foo_A3_Override6.
                Link[This.Foo.remove, Previous] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Current] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Foo), "A3" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_A3_Override8
        {
            add
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.add, Base] += value;

                // Should invoke this.Foo_A3_Override6.
                Link[This.Foo.add, Previous] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Current] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Final] += value;
            }
            remove
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.remove, Base] -= value;

                // Should invoke this.Foo_A3_Override6.
                Link[This.Foo.remove, Previous] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Current] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "A3" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Bar_A3_Override9
        {
            add
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.add, Base] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Previous] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Current] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Final] += value;
            }
            remove
            {
                // Should invoke this.Foo_A2_Override3.
                Link[This.Foo.remove, Base] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Previous] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Current] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "A4" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Bar_A4_Override10
        {
            add
            {
                // Should invoke this.Foo.
                Link[This.Foo.add, Base] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Previous] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Current] += value;

                // Should invoke this.Foo.
                Link[This.Foo.add, Final] += value;
            }
            remove
            {
                // Should invoke this.Foo.
                Link[This.Foo.remove, Base] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Previous] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Current] -= value;

                // Should invoke this.Foo.
                Link[This.Foo.remove, Final] -= value;
            }
        }
    }
}