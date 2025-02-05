using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.Source
{
    [PseudoLayerOrder("A1")]
    [PseudoLayerOrder("A2")]
    [PseudoLayerOrder("A3")]
    [PseudoLayerOrder("A4")]
    // <target>
    class Target
    {
        public int Foo
        {
            get
            {
                System.Console.WriteLine("This is original code (discarded).");

                return 42;
            }
            set
            {
                System.Console.WriteLine("This is original code (discarded).");
            }
        }

        public int Bar
        {
            get
            {
                System.Console.WriteLine("This is original code.");

                return 42;
            }
            set
            {
                System.Console.WriteLine("This is original code.");
            }
        }

        [PseudoOverride(nameof(Bar), "A1")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Bar_A1_Override1
        {
            get
            {
                // Should invoke this.Foo_Source.
                _ = link(_this.Foo.get, @base);
                // Should invoke this.Foo_Source.
                _ = link(_this.Foo.get, previous);
                // Should invoke this.Foo_Source.
                _ = link(_this.Foo.get, current);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, final);
                return 42;
            }
            set
            {
                // Should invoke this.Foo_Source.
                link[_this.Foo.set, @base] = value;
                // Should invoke this.Foo_Source.
                link[_this.Foo.set, previous] = value;
                // Should invoke this.Foo_Source.
                link[_this.Foo.set, current] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A2")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Bar_A2_Override2
        {
            get
            {
                // Should invoke this.Foo_Source.
                _ = link(_this.Foo.get, @base);
                // Should invoke this.Foo_Source.
                _ = link(_this.Foo.get, previous);
                // Should invoke this.Foo_A2_Override3.
                _ = link(_this.Foo.get, current);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, final);
                return 42;
            }
            set
            {
                // Should invoke this.Foo_Source.
                link[_this.Foo.set, @base] = value;
                // Should invoke this.Foo_Source.
                link[_this.Foo.set, previous] = value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.set, current] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Foo), "A2")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_A2_Override3
        {
            get
            {
                // Should invoke this.Foo_Source.
                _ = link(_this.Foo.get, @base);
                // Should invoke this.Foo_Source.
                _ = link(_this.Foo.get, previous);
                // Should invoke Foo_A2_Override3.
                _ = link(_this.Foo.get, current);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, final);
                return 42;
            }
            set
            {
                // Should invoke this.Foo_Source.
                link[_this.Foo.set, @base] = value;
                // Should invoke this.Foo_Source.
                link[_this.Foo.set, previous] = value;
                // Should invoke Foo_A2_Override3.
                link[_this.Foo.set, current] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A2")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Bar_A2_Override4
        {
            get
            {
                // Should invoke this.Foo_Source.
                _ = link(_this.Foo.get, @base);
                // Should invoke this.Foo_A2_Override3.
                _ = link(_this.Foo.get, previous);
                // Should invoke this.Foo_A2_Override3.
                _ = link(_this.Foo.get, current);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, final);
                return 42;
            }
            set
            {
                // Should invoke this.Foo_Source.
                link[_this.Foo.set, @base] = value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.set, previous] = value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.set, current] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Bar_A3_Override5
        {
            get
            {
                // Should invoke this.Foo_A2_Override3.
                _ = link(_this.Foo.get, @base);
                // Should invoke this.Foo_A2_Override3.
                _ = link(_this.Foo.get, previous);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, current);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, final);
                return 42;
            }
            set
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.set, @base] = value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.set, previous] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, current] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Foo), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_A3_Override6
        {
            get
            {
                // Should invoke this.Foo_A2_Override3.
                _ = link(_this.Foo.get, @base);
                // Should invoke this.Foo_A2_Override3.
                _ = link(_this.Foo.get, previous);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, current);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, final);
                return 42;
            }
            set
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.set, @base] = value;
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.set, previous] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, current] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Bar_A3_Override7
        {
            get
            {
                // Should invoke this.Foo_A2_Override3.
                _ = link(_this.Foo.get, @base);
                // Should invoke this.Foo_A3_Override6.
                _ = link(_this.Foo.get, previous);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, current);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, final);
                return 42;
            }
            set
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.set, @base] = value;
                // Should invoke this.Foo_A3_Override6.
                link[_this.Foo.set, previous] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, current] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Foo), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_A3_Override8
        {
            get
            {
                // Should invoke this.Foo_A2_Override3.
                _ = link(_this.Foo.get, @base);
                // Should invoke this.Foo_A3_Override6.
                _ = link(_this.Foo.get, previous);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, current);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, final);
                return 42;
            }
            set
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.set, @base] = value;
                // Should invoke this.Foo_A3_Override6.
                link[_this.Foo.set, previous] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, current] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Bar_A3_Override9
        {
            get
            {
                // Should invoke this.Foo_A2_Override3.
                _ = link(_this.Foo.get, @base);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, previous);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, current);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, final);
                return 42;
            }
            set
            {
                // Should invoke this.Foo_A2_Override3.
                link[_this.Foo.set, @base] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, previous] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, current] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A4")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Bar_A4_Override10
        {
            get
            {
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, @base);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, previous);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, current);
                // Should invoke this.Foo.
                _ = link(_this.Foo.get, final);
                return 42;
            }
            set
            {
                // Should invoke this.Foo.
                link[_this.Foo.set, @base] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, previous] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, current] = value;
                // Should invoke this.Foo.
                link[_this.Foo.set, final] = value;
            }
        }
    }
}