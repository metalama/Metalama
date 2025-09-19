// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.LinkerTests.Tests.EventFields.Linking.OverriddenLocalEvent
{
    [PseudoLayerOrder( "TestAspect0" )]
    [PseudoLayerOrder( "TestAspect1" )]
    [PseudoLayerOrder( "TestAspect2" )]
    [PseudoLayerOrder( "TestAspect3" )]
    [PseudoLayerOrder( "TestAspect4" )]
    [PseudoLayerOrder( "TestAspect5" )]
    [PseudoLayerOrder( "TestAspect6" )]

    // <target>
    internal class Target
    {
        public event EventHandler? Foo;

        [PseudoOverride( nameof(Foo), "TestAspect0" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_Override0
        {
            add
            {
                // Should invoke source code.
                Link[This.Bar.add, Base] += value;

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
                Link[This.Bar.remove, Base] -= value;

                // Should invoke source code.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke source code.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Foo), "TestAspect2" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_Override2
        {
            add
            {
                // Should invoke override 1.
                Link[This.Bar.add, Base] += value;

                // Should invoke override 1.
                Link[This.Bar.add, Previous] += value;

                // Should invoke override 1.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }

            remove
            {
                // Should invoke override 1.
                Link[This.Bar.remove, Base] -= value;

                // Should invoke override 1.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke override 1.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Foo), "TestAspect4" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_Override4
        {
            add
            {
                // Should invoke override 3.
                Link[This.Bar.add, Base] += value;

                // Should invoke override 3.
                Link[This.Bar.add, Previous] += value;

                // Should invoke override 3.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }

            remove
            {
                // Should invoke override 3.
                Link[This.Bar.remove, Base] -= value;

                // Should invoke override 3.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke override 3.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Foo), "TestAspect6" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_Override6
        {
            add
            {
                // Should invoke the final declaration.
                Link[This.Bar.add, Base] += value;

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
                Link[This.Bar.remove, Base] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        public event EventHandler? Bar;

        [PseudoOverride( nameof(Bar), "TestAspect1" )]
        [PseudoNotInlineable]
        public event EventHandler Bar_Override1
        {
            add
            {
                // Should invoke source code.
                Link[This.Bar.add, Base] += value;

                // Should invoke source code.
                Link[This.Bar.add, Previous] += value;

                // Should invoke override 1.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }

            remove
            {
                // Should invoke source code.
                Link[This.Bar.remove, Base] -= value;

                // Should invoke source code.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke override 1.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "TestAspect3" )]
        [PseudoNotInlineable]
        public event EventHandler Bar_Override3
        {
            add
            {
                // Should invoke override 1.
                Link[This.Bar.add, Base] += value;

                // Should invoke override 1.
                Link[This.Bar.add, Previous] += value;

                // Should invoke override 3.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }

            remove
            {
                // Should invoke override 1.
                Link[This.Bar.remove, Base] -= value;

                // Should invoke override 1.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke override 3.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "TestAspect5" )]
        [PseudoNotInlineable]
        public event EventHandler Bar_Override5
        {
            add
            {
                // Should invoke override 3.
                Link[This.Bar.add, Base] += value;

                // Should invoke override 3.
                Link[This.Bar.add, Previous] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }

            remove
            {
                // Should invoke override 3.
                Link[This.Bar.remove, Base] -= value;

                // Should invoke override 3.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }
    }
}