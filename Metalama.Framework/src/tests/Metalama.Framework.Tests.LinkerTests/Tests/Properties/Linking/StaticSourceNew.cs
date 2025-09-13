// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Linking.StaticSourceNew
{
    public class Base
    {
        public static int Bar
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
    internal
    // <target>
    class Target : Base
    {
        public static int Foo
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

        public static new int Bar
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

        [PseudoOverride(nameof(Foo), "A0")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public static int Foo_Override0
        {
            get
            {
                // Should invoke source code.
                _ = Link(Static.Target.Bar.get, Api.Base);
                // Should invoke source code.
                _ = Link(Static.Target.Bar.get, Previous );
                // Should invoke source code.
                _ = Link(Static.Target.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Final);
                return 42;
            }
            set
            {
                // Should invoke source code.
                Link[Static.Target.Bar.set, Api.Base] = value;
                // Should invoke source code.
                Link[Static.Target.Bar.set, Previous] = value;
                // Should invoke source code.
                Link[Static.Target.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Foo), "A2")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public static int Foo_Override2
        {
            get
            {
                // Should invoke override 1_2.
                _ = Link(Static.Target.Bar.get, Api.Base);
                // Should invoke override 1_2.
                _ = Link(Static.Target.Bar.get, Previous );
                // Should invoke override 1_2.
                _ = Link(Static.Target.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Final);
                return 42;
            }
            set
            {
                // Should invoke override 1_2.
                Link[Static.Target.Bar.set, Api.Base] = value;
                // Should invoke override 1_2.
                Link[Static.Target.Bar.set, Previous] = value;
                // Should invoke override 1_2.
                Link[Static.Target.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Foo), "A4")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public static int Foo_Override4
        {
            get
            {
                // Should invoke override 3_2.
                _ = Link(Static.Target.Bar.get, Api.Base);
                // Should invoke override 3_2.
                _ = Link(Static.Target.Bar.get, Previous );
                // Should invoke override 3_2.
                _ = Link(Static.Target.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Final);
                return 42;
            }
            set
            {
                // Should invoke override 3_2.
                Link[Static.Target.Bar.set, Api.Base] = value;
                // Should invoke override 3_2.
                Link[Static.Target.Bar.set, Previous] = value;
                // Should invoke override 3_2.
                Link[Static.Target.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Foo), "A6")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public static int Foo_Override6
        {
            get
            {
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Api.Base);
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Previous );
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Final);
                return 42;
            }
            set
            {
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Api.Base] = value;
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Previous] = value;
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A1")]
        [PseudoNotInlineable]
        private static int Bar_Override1_1
        {
            get
            {
                // Should invoke source code.
                _ = Link(Static.Target.Bar.get, Api.Base);
                // Should invoke source code.
                _ = Link(Static.Target.Bar.get, Previous );
                // Should invoke override 1_2.
                _ = Link(Static.Target.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Final);
                return 42;
            }
            set
            {
                // Should invoke source code.
                Link[Static.Target.Bar.set, Api.Base] = value;
                // Should invoke source code.
                Link[Static.Target.Bar.set, Previous] = value;
                // Should invoke override 1_2.
                Link[Static.Target.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A1")]
        [PseudoNotInlineable]
        private static int Bar_Override1_2
        {
            get
            {
                // Should invoke source code.
                _ = Link(Static.Target.Bar.get, Api.Base);
                // Should invoke override 1_1.
                _ = Link(Static.Target.Bar.get, Previous );
                // Should invoke override 1_2.
                _ = Link(Static.Target.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Final);
                return 42;
            }
            set
            {
                // Should invoke source code.
                Link[Static.Target.Bar.set, Api.Base] = value;
                // Should invoke override 1_1.
                Link[Static.Target.Bar.set, Previous] = value;
                // Should invoke override 1_2.
                Link[Static.Target.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        private static int Bar_Override3_1
        {
            get
            {
                // Should invoke override 1_2.
                _ = Link(Static.Target.Bar.get, Api.Base);
                // Should invoke override 1_2.
                _ = Link(Static.Target.Bar.get, Previous );
                // Should invoke override 3_2.
                _ = Link(Static.Target.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Final);
                return 42;
            }
            set
            {
                // Should invoke override 1_2.
                Link[Static.Target.Bar.set, Api.Base] = value;
                // Should invoke override 1_2.
                Link[Static.Target.Bar.set, Previous] = value;
                // Should invoke override 3_2.
                Link[Static.Target.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A3")]
        [PseudoNotInlineable]
        private static int Bar_Override3_2
        {
            get
            {
                // Should invoke override 1_2.
                _ = Link(Static.Target.Bar.get, Api.Base);
                // Should invoke override 3_1.
                _ = Link(Static.Target.Bar.get, Previous );
                // Should invoke override 3_2.
                _ = Link(Static.Target.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Final);
                return 42;
            }
            set
            {
                // Should invoke override 1_2.
                Link[Static.Target.Bar.set, Api.Base] = value;
                // Should invoke override 3_1.
                Link[Static.Target.Bar.set, Previous] = value;
                // Should invoke override 3_2.
                Link[Static.Target.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A5")]
        [PseudoNotInlineable]
        private static int Bar_Override5_1
        {
            get
            {
                // Should invoke override 3_2.
                _ = Link(Static.Target.Bar.get, Api.Base);
                // Should invoke override 3_2.
                _ = Link(Static.Target.Bar.get, Previous );
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Final);
                return 42;
            }
            set
            {
                // Should invoke override 3_2.
                Link[Static.Target.Bar.set, Api.Base] = value;
                // Should invoke override 3_2.
                Link[Static.Target.Bar.set, Previous] = value;
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Final] = value;
            }
        }

        [PseudoOverride(nameof(Bar), "A5")]
        [PseudoNotInlineable]
        private static int Bar_Override5_2
        {
            get
            {
                // Should invoke override 3_2.
                _ = Link(Static.Target.Bar.get, Api.Base);
                // Should invoke override 5_1.
                _ = Link(Static.Target.Bar.get, Previous );
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Current);
                // Should invoke the final declaration.
                _ = Link(Static.Target.Bar.get, Final);
                return 42;
            }
            set
            {
                // Should invoke override 3_2.
                Link[Static.Target.Bar.set, Api.Base] = value;
                // Should invoke override 5_1.
                Link[Static.Target.Bar.set, Previous] = value;
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Current] = value;
                // Should invoke the final declaration.
                Link[Static.Target.Bar.set, Final] = value;
            }
        }
    }
}