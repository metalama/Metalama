using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.IntroducedOverride
{
    class Base
    {
        public virtual int Bar
        {
            get
            {
                return 42;
            }
            set
            {
            }
        }
    }

    [PseudoLayerOrder("A0")]
    [PseudoLayerOrder("A1")]
    [PseudoLayerOrder("A2")]
    [PseudoLayerOrder("A3")]
    [PseudoLayerOrder("A4")]
    [PseudoLayerOrder("A5")]
    [PseudoLayerOrder("A6")]
    // <target>
    class Target : Base
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

        [PseudoIntroduction("A1")]
        [PseudoNotInlineable]
        public override int Bar
        {
            get
            {
                System.Console.WriteLine("SHOULD BE DISCARDED (this is introduced code).");

                return 42;
            }
            set
            {
                System.Console.WriteLine("SHOULD BE DISCARDED (this is introduced code).");
            }
        }

        [PseudoOverride(nameof(Foo), "A0")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_Override0
        {
            get
            {
                // Should invoke base declaration.
                _ = link(_this.Bar.get, @base);
                // Should invoke base declaration.
                _ = link(_this.Bar.get, previous);
                // Should invoke base declaration.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);
                return 42;
            }
            set
            {
                // Should invoke base declaration.
                link[_this.Bar.set, @base] = value;
                // Should invoke base declaration.
                link[_this.Bar.set, previous] = value;
                // Should invoke base declaration.
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
                // Should invoke override 1_2.
                _ = link(_this.Bar.get, @base);
                // Should invoke override 1_2.
                _ = link(_this.Bar.get, previous);
                // Should invoke override 1_2.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);
                return 42;
            }
            set
            {
                // Should invoke override 1_2.
                link[_this.Bar.set, @base] = value;
                // Should invoke override 1_2.
                link[_this.Bar.set, previous] = value;
                // Should invoke override 1_2.
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

        [PseudoOverride(nameof(Bar), "A1")]
        [PseudoNotInlineable]
        private int Bar_Override1_1
        {
            get
            {
                // Should invoke base declaration.
                _ = link(_this.Bar.get, @base);
                // Should invoke base declaration.
                _ = link(_this.Bar.get, previous);
                // Should invoke override 1_2.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);
                return 42;
            }
            set
            {
                // Should invoke base declaration.
                link[_this.Bar.set, @base] = value;
                // Should invoke base declaration.
                link[_this.Bar.set, previous] = value;
                // Should invoke override 1_2.
                link[_this.Bar.set, current] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A1")]
        [PseudoNotInlineable]
        private int Bar_Override1_2
        {
            get
            {
                // Should invoke base declaration.
                _ = link(_this.Bar.get, @base);
                // Should invoke override 1_1.
                _ = link(_this.Bar.get, previous);
                // Should invoke override 1_2.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);
                return 42;
            }
            set
            {
                // Should invoke base declaration.
                link[_this.Bar.set, @base] = value;
                // Should invoke override 1_1.
                link[_this.Bar.set, previous] = value;
                // Should invoke override 1_2.
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
                // Should invoke override 1_2.
                _ = link(_this.Bar.get, @base);
                // Should invoke override 1_2.
                _ = link(_this.Bar.get, previous);
                // Should invoke override 3_2.
                _ = link(_this.Bar.get, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar.get, final);
                return 42;
            }
            set
            {
                // Should invoke override 1_2.
                link[_this.Bar.set, @base] = value;
                // Should invoke override 1_2.
                link[_this.Bar.set, previous] = value;
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
                // Should invoke override 1_2.
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
                // Should invoke override 1_2.
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
                // Should invoke the final declaration.
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
                // Should invoke the final declaration.
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
                // Should invoke the final declaration.
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
                // Should invoke the final declaration.
                link[_this.Bar.set, current] = value;
                // Should invoke the final declaration.
                link[_this.Bar.set, final] = value;
            }
        }
    }
}
