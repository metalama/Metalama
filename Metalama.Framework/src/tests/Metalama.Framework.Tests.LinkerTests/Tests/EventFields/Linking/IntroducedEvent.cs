// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.LinkerTests.Tests.EventFields.Linking.IntroducedEvent
{
    [PseudoLayerOrder( "TestAspect0" )]
    [PseudoLayerOrder( "TestAspect1" )]
    [PseudoLayerOrder( "TestAspect2" )]
    [PseudoLayerOrder( "TestAspect3" )]
    [PseudoLayerOrder( "TestAspect4" )]
    [PseudoLayerOrder( "TestAspect5" )]
    [PseudoLayerOrder( "TestAspect6" )]
    [PseudoLayerOrder( "TestAspect7" )]

    // <target>
    internal class Target
    {
        public event EventHandler Foo
        {
            add => Console.WriteLine( "This is original code." );
            remove => Console.WriteLine( "This is original code." );
        }

        [PseudoOverride( nameof(Foo), "TestAspect0" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_Override0
        {
            add
            {
                // Should invoke empty code.
                Link[This.Bar.add, Base] += value;

                // Should invoke empty code.
                Link[This.Bar.add, Previous] += value;

                // Should invoke empty code.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }

            remove
            {
                // Should invoke empty code.
                Link[This.Bar.remove, Base] -= value;

                // Should invoke empty code.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke empty code.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Foo), "TestAspect3" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_Override3
        {
            add
            {
                // Should invoke override 2.
                Link[This.Bar.add, Base] += value;

                // Should invoke override 2.
                Link[This.Bar.add, Previous] += value;

                // Should invoke override 2.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }

            remove
            {
                // Should invoke override 2.
                Link[This.Bar.remove, Base] -= value;

                // Should invoke override 2.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke override 2.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Foo), "TestAspect5" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_Override5
        {
            add
            {
                // Should invoke override 4.
                Link[This.Bar.add, Base] += value;

                // Should invoke override 4.
                Link[This.Bar.add, Previous] += value;

                // Should invoke override 4.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }

            remove
            {
                // Should invoke override 4.
                Link[This.Bar.remove, Base] -= value;

                // Should invoke override 4.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke override 4.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Foo), "TestAspect7" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event EventHandler Foo_Override7
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

        [PseudoIntroduction( "TestAspect1" )]
        [PseudoNotInlineable]
        public event EventHandler? Bar;

        [PseudoOverride( nameof(Bar), "TestAspect2" )]
        [PseudoNotInlineable]
        private event EventHandler? Bar_Override2
        {
            add
            {
                // Should invoke source code.
                Link[This.Bar.add, Base] += value;

                // Should invoke source code.
                Link[This.Bar.add, Previous] += value;

                // Should invoke override 2.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }

            remove
            {
                // Should invoke introduced event field.
                Link[This.Bar.remove, Base] -= value;

                // Should invoke introduced event field.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke override 2.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "TestAspect4" )]
        [PseudoNotInlineable]
        private event EventHandler? Bar_Override4
        {
            add
            {
                // Should invoke override 2.
                Link[This.Bar.add, Base] += value;

                // Should invoke override 2.
                Link[This.Bar.add, Previous] += value;

                // Should invoke override 4.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }

            remove
            {
                // Should invoke override 2.
                Link[This.Bar.remove, Base] -= value;

                // Should invoke override 2.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke override 4.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "TestAspect6" )]
        [PseudoNotInlineable]
        private event EventHandler? Bar_Override6
        {
            add
            {
                // Should invoke override 4.
                Link[This.Bar.add, Base] += value;

                // Should invoke override 4.
                Link[This.Bar.add, Previous] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Current] += value;

                // Should invoke the final declaration.
                Link[This.Bar.add, Final] += value;
            }

            remove
            {
                // Should invoke override 4.
                Link[This.Bar.remove, Base] -= value;

                // Should invoke override 4.
                Link[This.Bar.remove, Previous] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Current] -= value;

                // Should invoke the final declaration.
                Link[This.Bar.remove, Final] -= value;
            }
        }
    }
}