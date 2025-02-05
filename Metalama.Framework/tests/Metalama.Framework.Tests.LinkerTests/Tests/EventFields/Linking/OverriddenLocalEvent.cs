// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
                link[_this.Bar.add, @base] += value;

                // Should invoke source code.
                link[_this.Bar.add, previous] += value;

                // Should invoke source code.
                link[_this.Bar.add, current] += value;

                // Should invoke the final declaration.
                link[_this.Bar.add, final] += value;
            }

            remove
            {
                // Should invoke source code.
                link[_this.Bar.remove, @base] -= value;

                // Should invoke source code.
                link[_this.Bar.remove, previous] -= value;

                // Should invoke source code.
                link[_this.Bar.remove, current] -= value;

                // Should invoke the final declaration.
                link[_this.Bar.remove, final] -= value;
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
                link[_this.Bar.add, @base] += value;

                // Should invoke override 1.
                link[_this.Bar.add, previous] += value;

                // Should invoke override 1.
                link[_this.Bar.add, current] += value;

                // Should invoke the final declaration.
                link[_this.Bar.add, final] += value;
            }

            remove
            {
                // Should invoke override 1.
                link[_this.Bar.remove, @base] -= value;

                // Should invoke override 1.
                link[_this.Bar.remove, previous] -= value;

                // Should invoke override 1.
                link[_this.Bar.remove, current] -= value;

                // Should invoke the final declaration.
                link[_this.Bar.remove, final] -= value;
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
                link[_this.Bar.add, @base] += value;

                // Should invoke override 3.
                link[_this.Bar.add, previous] += value;

                // Should invoke override 3.
                link[_this.Bar.add, current] += value;

                // Should invoke the final declaration.
                link[_this.Bar.add, final] += value;
            }

            remove
            {
                // Should invoke override 3.
                link[_this.Bar.remove, @base] -= value;

                // Should invoke override 3.
                link[_this.Bar.remove, previous] -= value;

                // Should invoke override 3.
                link[_this.Bar.remove, current] -= value;

                // Should invoke the final declaration.
                link[_this.Bar.remove, final] -= value;
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
                link[_this.Bar.add, @base] += value;

                // Should invoke the final declaration.
                link[_this.Bar.add, previous] += value;

                // Should invoke the final declaration.
                link[_this.Bar.add, current] += value;

                // Should invoke the final declaration.
                link[_this.Bar.add, final] += value;
            }

            remove
            {
                // Should invoke the final declaration.
                link[_this.Bar.remove, @base] -= value;

                // Should invoke the final declaration.
                link[_this.Bar.remove, previous] -= value;

                // Should invoke the final declaration.
                link[_this.Bar.remove, current] -= value;

                // Should invoke the final declaration.
                link[_this.Bar.remove, final] -= value;
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
                link[_this.Bar.add, @base] += value;

                // Should invoke source code.
                link[_this.Bar.add, previous] += value;

                // Should invoke override 1.
                link[_this.Bar.add, current] += value;

                // Should invoke the final declaration.
                link[_this.Bar.add, final] += value;
            }

            remove
            {
                // Should invoke source code.
                link[_this.Bar.remove, @base] -= value;

                // Should invoke source code.
                link[_this.Bar.remove, previous] -= value;

                // Should invoke override 1.
                link[_this.Bar.remove, current] -= value;

                // Should invoke the final declaration.
                link[_this.Bar.remove, final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "TestAspect3" )]
        [PseudoNotInlineable]
        public event EventHandler Bar_Override3
        {
            add
            {
                // Should invoke override 1.
                link[_this.Bar.add, @base] += value;

                // Should invoke override 1.
                link[_this.Bar.add, previous] += value;

                // Should invoke override 3.
                link[_this.Bar.add, current] += value;

                // Should invoke the final declaration.
                link[_this.Bar.add, final] += value;
            }

            remove
            {
                // Should invoke override 1.
                link[_this.Bar.remove, @base] -= value;

                // Should invoke override 1.
                link[_this.Bar.remove, previous] -= value;

                // Should invoke override 3.
                link[_this.Bar.remove, current] -= value;

                // Should invoke the final declaration.
                link[_this.Bar.remove, final] -= value;
            }
        }

        [PseudoOverride( nameof(Bar), "TestAspect5" )]
        [PseudoNotInlineable]
        public event EventHandler Bar_Override5
        {
            add
            {
                // Should invoke override 3.
                link[_this.Bar.add, @base] += value;

                // Should invoke override 3.
                link[_this.Bar.add, previous] += value;

                // Should invoke the final declaration.
                link[_this.Bar.add, current] += value;

                // Should invoke the final declaration.
                link[_this.Bar.add, final] += value;
            }

            remove
            {
                // Should invoke override 3.
                link[_this.Bar.remove, @base] -= value;

                // Should invoke override 3.
                link[_this.Bar.remove, previous] -= value;

                // Should invoke the final declaration.
                link[_this.Bar.remove, current] -= value;

                // Should invoke the final declaration.
                link[_this.Bar.remove, final] -= value;
            }
        }
    }
}