// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0109

namespace Metalama.Framework.Tests.LinkerTests.Tests.Fields.Linking.IntroducedNew
{
    internal class Base
    {
        public int Bar;
    }

    [PseudoLayerOrder("A0")]
    [PseudoLayerOrder("A1")]
    [PseudoLayerOrder("A2")]
    [PseudoLayerOrder("A3")]
    [PseudoLayerOrder("A4")]
    [PseudoLayerOrder("A5")]
    [PseudoLayerOrder("A6")]
    [PseudoLayerOrder("A7")]
    [PseudoLayerOrder("A8")]
    internal
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

        [PseudoReplaced]
        [PseudoIntroduction("A1")]
        public new int Bar;

        [PseudoReplacement(nameof(Bar))]
        [PseudoIntroduction("A3")]
        public new int Bar_Replacement { get; set; }

        [PseudoOverride(nameof(Foo), "A0")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_Override0
        {
            get
            {
                // Should invoke base declaration.
                _ = Link(This.Bar.get, Api.Base);
                // Should invoke base declaration.
                _ = Link(This.Bar.get, Previous );
                // Should invoke base declaration.
                _ = Link(This.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Final);

                return 42;
            }
            set
            {
                // Should invoke base declaration.
                Link[This.Bar.set, Api.Base] = value;
                // Should invoke base declaration.
                Link[This.Bar.set, Previous] = value;
                // Should invoke base declaration.
                Link[This.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Final] = value;
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
                _ = Link(This.Bar.get, Api.Base);
                // Should invoke backing field.
                _ = Link(This.Bar.get, Previous );
                // Should invoke backing field.
                _ = Link(This.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Final);

                return 42;
            }
            set
            {
                // Should invoke backing field.
                Link[This.Bar.set, Api.Base] = value;
                // Should invoke backing field.
                Link[This.Bar.set, Previous] = value;
                // Should invoke backing field.
                Link[This.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Final] = value;
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
                _ = Link(This.Bar.get, Api.Base);
                // Should invoke override 3_2.
                _ = Link(This.Bar.get, Previous );
                // Should invoke override 3_2.
                _ = Link(This.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Final);

                return 42;
            }
            set
            {
                // Should invoke override 3_2.
                Link[This.Bar.set, Api.Base] = value;
                // Should invoke override 3_2.
                Link[This.Bar.set, Previous] = value;
                // Should invoke override 3_2.
                Link[This.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Final] = value;
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
                _ = Link(This.Bar.get, Api.Base);
                // Should invoke override 5_2.
                _ = Link(This.Bar.get, Previous );
                // Should invoke override 5_2.
                _ = Link(This.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Final);

                return 42;
            }
            set
            {
                // Should invoke override 5_2.
                Link[This.Bar.set, Api.Base] = value;
                // Should invoke override 5_2.
                Link[This.Bar.set, Previous] = value;
                // Should invoke override 5_2.
                Link[This.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Final] = value;
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
                _ = Link(This.Bar.get, Api.Base);
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Previous );
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Final);

                return 42;
            }
            set
            {
                // Should invoke the final declaration.
                Link[This.Bar.set, Api.Base] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Previous] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        private int Bar_Override3_1
        {
            get
            {
                // Should invoke backing field.
                _ = Link(This.Bar.get, Api.Base);
                // Should invoke backing field.
                _ = Link(This.Bar.get, Previous );
                // Should invoke override 3_2.
                _ = Link(This.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Final);

                return 42;
            }
            set
            {
                // Should invoke backing field.
                Link[This.Bar.set, Api.Base] = value;
                // Should invoke backing field.
                Link[This.Bar, Previous] = value;
                // Should invoke override 3_2.
                Link[This.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        private int Bar_Override3_2
        {
            get
            {
                // Should invoke backing field.
                _ = Link(This.Bar.get, Api.Base);
                // Should invoke override 3_1.
                _ = Link(This.Bar.get, Previous );
                // Should invoke override 3_2.
                _ = Link(This.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Final);

                return 42;
            }
            set
            {
                // Should invoke backing field.
                Link[This.Bar.set, Api.Base] = value;
                // Should invoke override 3_1.
                Link[This.Bar.set, Previous] = value;
                // Should invoke override 3_2.
                Link[This.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A5")]
        [PseudoNotInlineable]
        private int Bar_Override5_1
        {
            get
            {
                // Should invoke override 3_2.
                _ = Link(This.Bar.get, Api.Base);
                // Should invoke override 3_2.
                _ = Link(This.Bar.get, Previous );
                // Should invoke override 5_2.
                _ = Link(This.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Final);

                return 42;
            }
            set
            {
                // Should invoke override 3_2.
                Link[This.Bar.set, Api.Base] = value;
                // Should invoke override 3_2.
                Link[This.Bar.set, Previous] = value;
                // Should invoke override 5_2.
                Link[This.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A5")]
        [PseudoNotInlineable]
        private int Bar_Override5_2
        {
            get
            {
                // Should invoke override 3_2.
                _ = Link(This.Bar.get, Api.Base);
                // Should invoke override 5_1.
                _ = Link(This.Bar.get, Previous );
                // Should invoke override 5_2.
                _ = Link(This.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Final);

                return 42;
            }
            set
            {
                // Should invoke override 3_2.
                Link[This.Bar.set, Api.Base] = value;
                // Should invoke override 5_1.
                Link[This.Bar.set, Previous] = value;
                // Should invoke override 5_2.
                Link[This.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A7")]
        [PseudoNotInlineable]
        private int Bar_Override7_1
        {
            get
            {
                // Should invoke override 5_2.
                _ = Link(This.Bar.get, Api.Base);
                // Should invoke override 5_2.
                _ = Link(This.Bar.get, Previous );
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Final);

                return 42;
            }
            set
            {
                // Should invoke override 5_2.
                Link[This.Bar.set, Api.Base] = value;
                // Should invoke override 5_2.
                Link[This.Bar.set, Previous] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A7")]
        [PseudoNotInlineable]
        private int Bar_Override7_2
        {
            get
            {
                // Should invoke override 5_2.
                _ = Link(This.Bar.get, Api.Base);
                // Should invoke override 7_1.
                _ = Link(This.Bar.get, Previous );
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(This.Bar.get, Final);

                return 42;
            }
            set
            {
                // Should invoke override 5_2.
                Link[This.Bar.set, Api.Base] = value;
                // Should invoke override 7_1.
                Link[This.Bar.set, Previous] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[This.Bar.set, Final] = value;
            }
        }
    }
}
