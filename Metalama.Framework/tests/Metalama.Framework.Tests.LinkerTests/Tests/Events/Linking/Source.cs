using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Events.Linking.Source
{
    [PseudoLayerOrder("A1")]
    [PseudoLayerOrder("A2")]
    [PseudoLayerOrder("A3")]
    [PseudoLayerOrder("A4")]
    // <target>
    class Target
    {
        public event System.EventHandler Foo
        {
            add
            {
                System.Console.WriteLine("This is original code (discarded).");
            }
            remove
            {
                System.Console.WriteLine("This is original code (discarded).");
            }
        }

        public event System.EventHandler Bar
        {
            add
            {
                System.Console.WriteLine("This is original code.");
            }
            remove
            {
                System.Console.WriteLine("This is original code.");
            }
        }

        [PseudoOverride(nameof(Bar), "A1")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event System.EventHandler Bar_A1_Override1
        {
            add
            {
                // Should invoke this.Foo_Source.
                link[_this.Foo.add, @base] += value;
                // Should invoke this.Foo_Source.
                link[_this.Foo.add, previous] += value;
                // Should invoke this.Foo_Source.
                link[_this.Foo.add, current] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, final] += value;
            }
            remove
            {
                // Should invoke this.Foo_Source.
                link[_this.Foo.remove, @base] -= value;
                // Should invoke this.Foo_Source.
                link[_this.Foo.remove, previous] -= value;
                // Should invoke this.Foo_Source.
                link[_this.Foo.remove, current] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, final] -= value;
            }
        }

        [PseudoOverride(nameof(Bar), "A2")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event System.EventHandler Bar_A2_Override2
        {
            add
            {
                // Should invoke this.Foo_Source.
                link[_this.Foo.add, @base] += value;
                // Should invoke this.Foo_Source.
                link[_this.Foo.add, previous] += value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.add, current] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, final] += value;
            }
            remove
            {
                // Should invoke this.Foo_Source.
                link[_this.Foo.remove, @base] -= value;
                // Should invoke this.Foo_Source.
                link[_this.Foo.remove, previous] -= value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.remove, current] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, final] -= value;
            }
        }

        [PseudoOverride(nameof(Foo), "A2")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event System.EventHandler Foo_A2_Override3
        {
            add
            {
                // Should invoke this.Foo_Source.
                link[_this.Foo.add, @base] += value;
                // Should invoke this.Foo_Source.
                link[_this.Foo.add, previous] += value;
                // Should invoke Foo_A2_Override3.
                link[_this.Foo.add, current] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, final] += value;
            }
            remove
            {
                // Should invoke this.Foo_Source.
                link[_this.Foo.remove, @base] -= value;
                // Should invoke this.Foo_Source.
                link[_this.Foo.remove, previous] -= value;
                // Should invoke Foo_A2_Override3.
                link[_this.Foo.remove, current] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, final] -= value;
            }
        }

        [PseudoOverride(nameof(Bar), "A2")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event System.EventHandler Bar_A2_Override4
        {
            add
            {
                // Should invoke this.Foo_Source.
                link[_this.Foo.add, @base] += value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.add, previous] += value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.add, current] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, final] += value;
            }
            remove
            {
                // Should invoke this.Foo_Source.
                link[_this.Foo.remove, @base] -= value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.remove, previous] -= value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.remove, current] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, final] -= value;
            }
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event System.EventHandler Bar_A3_Override5
        {
            add
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.add, @base] += value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.add, previous] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, current] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, final] += value;
            }
            remove
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.remove, @base] -= value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.remove, previous] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, current] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, final] -= value;
            }
        }

        [PseudoOverride(nameof(Foo), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event System.EventHandler Foo_A3_Override6
        {
            add
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.add, @base] += value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.add, previous] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, current] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, final] += value;
            }
            remove
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.remove, @base] -= value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.remove, previous] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, current] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, final] -= value;
            }
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event System.EventHandler Bar_A3_Override7
        {
            add
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.add, @base] += value;
                // Should invoke this.Foo_A3_Override6.
                link[_this.Foo.add, previous] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, current] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, final] += value;
            }
            remove
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.remove, @base] -= value;
                // Should invoke this.Foo_A3_Override6.
                link[_this.Foo.remove, previous] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, current] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, final] -= value;
            }
        }

        [PseudoOverride(nameof(Foo), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event System.EventHandler Foo_A3_Override8
        {
            add
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.add, @base] += value;
                // Should invoke this.Foo_A3_Override6.
                link[_this.Foo.add, previous] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, current] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, final] += value;
            }
            remove
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.remove, @base] -= value;
                // Should invoke this.Foo_A3_Override6.
                link[_this.Foo.remove, previous] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, current] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, final] -= value;
            }
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event System.EventHandler Bar_A3_Override9
        {
            add
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.add, @base] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, previous] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, current] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, final] += value;
            }
            remove
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.remove, @base] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, previous] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, current] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, final] -= value;
            }
        }

        [PseudoOverride(nameof(Bar), "A4")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public event System.EventHandler Bar_A4_Override10
        {
            add
            {
                // Should invoke this.Foo.
                link[_this.Foo.add, @base] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, previous] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, current] += value;
                // Should invoke this.Foo.
                link[_this.Foo.add, final] += value;
            }
            remove
            {
                // Should invoke this.Foo.
                link[_this.Foo.remove, @base] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, previous] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, current] -= value;
                // Should invoke this.Foo.
                link[_this.Foo.remove, final] -= value;
            }
        }
    }
}