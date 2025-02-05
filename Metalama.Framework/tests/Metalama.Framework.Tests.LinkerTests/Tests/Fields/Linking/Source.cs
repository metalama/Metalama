using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Fields.Linking.Source
{
    [PseudoLayerOrder("A0")]
    [PseudoLayerOrder("A2")]
    [PseudoLayerOrder("A3")]
    [PseudoLayerOrder("A4")]
    [PseudoLayerOrder("A5")]
    [PseudoLayerOrder("A6")]
    [PseudoLayerOrder("A7")]
    [PseudoLayerOrder("A8")]
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

        public int Bar;

        [PseudoReplacement(nameof(Bar))]
        [PseudoIntroduction("A3")]
        public int Bar_Replacement { get; set; }

        [PseudoOverride(nameof(Foo), "A0")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_Override0
        {
            get
            {
                // Should invoke backing field.
                _ = link(_this.Bar.get, @base);
                // Should invoke backing field.
                _ = link(_this.Bar.get, previous);
                // Should invoke backing field.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);

                return 42;
            }
            set
            {
                // Should invoke backing field.
                link[_this.Bar.set, @base] = value;
                // Should invoke backing field.
                link[_this.Bar.set, previous] = value;
                // Should invoke backing field.
                link[_this.Bar.set, current] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Foo), "A2")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_Override2
        {
            get
            {
                // Should invoke backing field.
                _ = link(_this.Bar.get, @base);
                // Should invoke backing field.
                _ = link(_this.Bar.get, previous);
                // Should invoke backing field.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);

                return 42;
            }
            set
            {
                // Should invoke backing field.
                link[_this.Bar.set, @base] = value;
                // Should invoke backing field.
                link[_this.Bar.set, previous] = value;
                // Should invoke backing field.
                link[_this.Bar.set, current] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Foo), "A4")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_Override4
        {
            get
            {
                // Should invoke override 3_2.
                _ = link(_this.Bar.get, @base);
                // Should invoke override 3_2.
                _ = link(_this.Bar.get, previous);
                // Should invoke override 3_2.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);

                return 42;
            }
            set
            {
                // Should invoke override 3_2.
                link[_this.Bar.set, @base] = value;
                // Should invoke override 3_2.
                link[_this.Bar.set, previous] = value;
                // Should invoke override 3_2.
                link[_this.Bar.set, current] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Foo), "A6")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_Override6
        {
            get
            {
                // Should invoke override 5_2.
                _ = link(_this.Bar.get, @base);
                // Should invoke override 5_2.
                _ = link(_this.Bar.get, previous);
                // Should invoke override 5_2.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);

                return 42;
            }
            set
            {
                // Should invoke override 5_2.
                link[_this.Bar.set, @base] = value;
                // Should invoke override 5_2.
                link[_this.Bar.set, previous] = value;
                // Should invoke override 5_2.
                link[_this.Bar.set, current] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Foo), "A8")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_Override8
        {
            get
            {
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, @base);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, previous);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);

                return 42;
            }
            set
            {
                // Should invoke the final declaration.
                link[_this.Bar.set, @base] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, previous] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, current] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        private int Bar_Override3_1
        {
            get
            {
                // Should invoke backing field.
                _ = link(_this.Bar.get, @base);
                // Should invoke backing field.
                _ = link(_this.Bar.get, previous);
                // Should invoke override 3_2.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);

                return 42;
            }
            set
            {
                // Should invoke backing field.
                link[_this.Bar.set, @base] = value;
                // Should invoke backing field.
                link[_this.Bar, previous] = value;
                // Should invoke override 3_2.
                link[_this.Bar.set, current] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        private int Bar_Override3_2
        {
            get
            {
                // Should invoke backing field.
                _ = link(_this.Bar.get, @base);
                // Should invoke override 3_1.
                _ = link(_this.Bar.get, previous);
                // Should invoke override 3_2.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);

                return 42;
            }
            set
            {
                // Should invoke backing field.
                link[_this.Bar.set, @base] = value;
                // Should invoke override 3_1.
                link[_this.Bar.set, previous] = value;
                // Should invoke override 3_2.
                link[_this.Bar.set, current] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A5")]
        [PseudoNotInlineable]
        private int Bar_Override5_1
        {
            get
            {
                // Should invoke override 3_2.
                _ = link(_this.Bar.get, @base);
                // Should invoke override 3_2.
                _ = link(_this.Bar.get, previous);
                // Should invoke override 5_2.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);

                return 42;
            }
            set
            {
                // Should invoke override 3_2.
                link[_this.Bar.set, @base] = value;
                // Should invoke override 3_2.
                link[_this.Bar.set, previous] = value;
                // Should invoke override 5_2.
                link[_this.Bar.set, current] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A5")]
        [PseudoNotInlineable]
        private int Bar_Override5_2
        {
            get
            {
                // Should invoke override 3_2.
                _ = link(_this.Bar.get, @base);
                // Should invoke override 5_1.
                _ = link(_this.Bar.get, previous);
                // Should invoke override 5_2.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);

                return 42;
            }
            set
            {
                // Should invoke override 3_2.
                link[_this.Bar.set, @base] = value;
                // Should invoke override 5_1.
                link[_this.Bar.set, previous] = value;
                // Should invoke override 5_2.
                link[_this.Bar.set, current] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A7")]
        [PseudoNotInlineable]
        private int Bar_Override7_1
        {
            get
            {
                // Should invoke override 5_2.
                _ = link(_this.Bar.get, @base);
                // Should invoke override 5_2.
                _ = link(_this.Bar.get, previous);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);

                return 42;
            }
            set
            {
                // Should invoke override 5_2.
                link[_this.Bar.set, @base] = value;
                // Should invoke override 5_2.
                link[_this.Bar.set, previous] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, current] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A7")]
        [PseudoNotInlineable]
        private int Bar_Override7_2
        {
            get
            {
                // Should invoke override 5_2.
                _ = link(_this.Bar.get, @base);
                // Should invoke override 7_1.
                _ = link(_this.Bar.get, previous);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);

                return 42;
            }
            set
            {
                // Should invoke override 5_2.
                link[_this.Bar.set, @base] = value;
                // Should invoke override 7_1.
                link[_this.Bar.set, previous] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, current] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, final] = value;
            }
        }
    }
}
