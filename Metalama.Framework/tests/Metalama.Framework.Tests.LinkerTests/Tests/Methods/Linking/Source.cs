using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.Source
{
    [PseudoLayerOrder("A1")]
    [PseudoLayerOrder("A2")]
    [PseudoLayerOrder("A3")]
    [PseudoLayerOrder("A4")]
    // <target>
    class Target
    {
        public void Foo()
        {
            System.Console.WriteLine("This is original code (discarded).");
        }

        public void Bar()
        {
            Console.WriteLine("This is original code.");
        }

        [PseudoOverride(nameof(Bar), "A1")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public void Bar_A1_Override1()
        {
            // Should invoke this.Foo_Source.
            link(_this.Foo, @base)();
            // Should invoke this.Foo_Source.
            link(_this.Foo, previous)();
            // Should invoke this.Foo_Source.
            link(_this.Foo, current)();
            // Should invoke this.Foo.
            link(_this.Foo, final)();
        }

        [PseudoOverride(nameof(Bar), "A2")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public void Bar_A2_Override2()
        {
            // Should invoke this.Foo_Source.
            link(_this.Foo, @base)();
            // Should invoke this.Foo_Source.
            link(_this.Foo, previous)();
            // Should invoke this.Foo_A2_Override3.
            link(_this.Foo, current)();
            // Should invoke this.Foo.
            link(_this.Foo, final)();
        }

        [PseudoOverride(nameof(Foo), "A2")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public void Foo_A2_Override3()
        {
            // Should invoke this.Foo_Source.
            link(_this.Foo, @base)();
            // Should invoke this.Foo_Source.
            link(_this.Foo, previous)();
            // Should invoke Foo_A2_Override3.
            link(_this.Foo, current)();
            // Should invoke this.Foo.
            link(_this.Foo, final)();
        }

        [PseudoOverride(nameof(Bar), "A2")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public void Bar_A2_Override4()
        {
            // Should invoke this.Foo_Source.
            link(_this.Foo, @base)();
            // Should invoke this.Foo_A2_Override3.
            link(_this.Foo, previous)();
            // Should invoke this.Foo_A2_Override3.
            link(_this.Foo, current)();
            // Should invoke this.Foo.
            link(_this.Foo, final)();
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public void Bar_A3_Override5()
        {
            // Should invoke this.Foo_A2_Override3.
            link(_this.Foo, @base)();
            // Should invoke this.Foo_A2_Override3.
            link(_this.Foo, previous)();
            // Should invoke this.Foo.
            link(_this.Foo, current)();
            // Should invoke this.Foo.
            link(_this.Foo, final)();
        }

        [PseudoOverride(nameof(Foo), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public void Foo_A3_Override6()
        {
            // Should invoke this.Foo_A2_Override3.
            link(_this.Foo, @base)();
            // Should invoke this.Foo_A2_Override3.
            link(_this.Foo, previous)();
            // Should invoke this.Foo.
            link(_this.Foo, current)();
            // Should invoke this.Foo.
            link(_this.Foo, final)();
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public void Bar_A3_Override7()
        {
            // Should invoke this.Foo_A2_Override3.
            link(_this.Foo, @base)();
            // Should invoke this.Foo_A3_Override6.
            link(_this.Foo, previous)();
            // Should invoke this.Foo.
            link(_this.Foo, current)();
            // Should invoke this.Foo.
            link(_this.Foo, final)();
        }

        [PseudoOverride(nameof(Foo), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public void Foo_A3_Override8()
        {
            // Should invoke this.Foo_A2_Override3.
            link(_this.Foo, @base)();
            // Should invoke this.Foo_A3_Override6.
            link(_this.Foo, previous)();
            // Should invoke this.Foo.
            link(_this.Foo, current)();
            // Should invoke this.Foo.
            link(_this.Foo, final)();
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public void Bar_A3_Override9()
        {
            // Should invoke this.Foo_A2_Override3.
            link(_this.Foo, @base)();
            // Should invoke this.Foo.
            link(_this.Foo, previous)();
            // Should invoke this.Foo.
            link(_this.Foo, current)();
            // Should invoke this.Foo.
            link(_this.Foo, final)();
        }

        [PseudoOverride(nameof(Bar), "A4")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public void Bar_A4_Override10()
        {
            // Should invoke this.Foo.
            link(_this.Foo, @base)();
            // Should invoke this.Foo.
            link(_this.Foo, previous)();
            // Should invoke this.Foo.
            link(_this.Foo, current)();
            // Should invoke this.Foo.
            link(_this.Foo, final)();
        }
    }
}
