// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.SourceOverride
{
    internal class Base
    {
        public virtual event EventHandler Bar
        {
            add { }
            remove { }
        }
    }

    [PseudoLayerOrder( "A0" )]
    [PseudoLayerOrder( "A1" )]
    [PseudoLayerOrder( "A2" )]
    [PseudoLayerOrder( "A3" )]
    [PseudoLayerOrder( "A4" )]
    [PseudoLayerOrder( "A5" )]
    [PseudoLayerOrder( "A6" )]

    // <target>
    internal class Target : Base
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

        public override event EventHandler Bar
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

        [PseudoOverride( nameof(Foo), "A0" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_Override0
        {
            add
            {
                // Should invoke source code.
                Link[This.Bar.add, Api.Base] += value;

                // Should invoke source code.
                Link[This.Bar.add, Previous] += value;

                // Should invoke source code.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }
            remove
            {
                // Should invoke source code.
                Link[This.Bar.remove, Api.Base] -= value;

                // Should invoke source code.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke source code.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Foo), "A2" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_Override2
        {
            add
            {
                // Should invoke override 1_2.
                Link[This.Bar.add, Api.Base] += value;

                // Should invoke override 1_2.
                Link[This.Bar.add, Previous] += value;

                // Should invoke override 1_2.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }
            remove
            {
                // Should invoke override 1_2.
                Link[This.Bar.remove, Api.Base] -= value;

                // Should invoke override 1_2.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke override 1_2.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Foo), "A4" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_Override4
        {
            add
            {
                // Should invoke override 3_2.
                Link[This.Bar.add, Api.Base] += value;

                // Should invoke override 3_2.
                Link[This.Bar.add, Previous] += value;

                // Should invoke override 3_2.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }
            remove
            {
                // Should invoke override 3_2.
                Link[This.Bar.remove, Api.Base] -= value;

                // Should invoke override 3_2.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke override 3_2.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Foo), "A6" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_Override6
        {
            add
            {
                // Should invoke the final declaration.
                Link[This.Bar.add, Api.Base] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Previous] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }
            remove
            {
                // Should invoke the final declaration.
                Link[This.Bar.remove, Api.Base] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "A1" )]
        [PseudoNotInlineable]
        private event EventHandler Bar_Override1_1
        {
            add
            {
                // Should invoke source code.
                Link[This.Bar.add, Api.Base] += value;

                // Should invoke source code.
                Link[This.Bar.add, Previous] += value;

                // Should invoke override 1_2.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }
            remove
            {
                // Should invoke source code.
                Link[This.Bar.remove, Api.Base] -= value;

                // Should invoke source code.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke override 1_2.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "A1" )]
        [PseudoNotInlineable]
        private event EventHandler Bar_Override1_2
        {
            add
            {
                // Should invoke source code.
                Link[This.Bar.add, Api.Base] += value;

                // Should invoke override 1_1.
                Link[This.Bar.add, Previous] += value;

                // Should invoke override 1_2.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }
            remove
            {
                // Should invoke source code.
                Link[This.Bar.remove, Api.Base] -= value;

                // Should invoke override 1_1.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke override 1_2.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "A3" )]
        [PseudoNotInlineable]
        private event EventHandler Bar_Override3_1
        {
            add
            {
                // Should invoke override 1_2.
                Link[This.Bar.add, Api.Base] += value;

                // Should invoke override 1_2.
                Link[This.Bar.add, Previous] += value;

                // Should invoke override 3_2.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }
            remove
            {
                // Should invoke override 1_2.
                Link[This.Bar.remove, Api.Base] -= value;

                // Should invoke override 1_2.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke override 3_2.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "A3" )]
        [PseudoNotInlineable]
        private event EventHandler Bar_Override3_2
        {
            add
            {
                // Should invoke override 1_2.
                Link[This.Bar.add, Api.Base] += value;

                // Should invoke override 3_1.
                Link[This.Bar.add, Previous] += value;

                // Should invoke override 3_2.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }
            remove
            {
                // Should invoke override 1_2.
                Link[This.Bar.remove, Api.Base] -= value;

                // Should invoke override 3_1.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke override 3_2.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "A5" )]
        [PseudoNotInlineable]
        private event EventHandler Bar_Override5_1
        {
            add
            {
                // Should invoke override 3_2.
                Link[This.Bar.add, Api.Base] += value;

                // Should invoke override 3_2.
                Link[This.Bar.add, Previous] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }
            remove
            {
                // Should invoke override 3_2.
                Link[This.Bar.remove, Api.Base] -= value;

                // Should invoke override 3_2.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "A5" )]
        [PseudoNotInlineable]
        private event EventHandler Bar_Override5_2
        {
            add
            {
                // Should invoke override 3_2.
                Link[This.Bar.add, Api.Base] += value;

                // Should invoke override 5_1.
                Link[This.Bar.add, Previous] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }
            remove
            {
                // Should invoke override 3_2.
                Link[This.Bar.remove, Api.Base] -= value;

                // Should invoke override 5_1.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }
    }
}