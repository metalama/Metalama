// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Linking.StaticIntroduced
{
    [PseudoLayerOrder( "A0" )]
    [PseudoLayerOrder( "A1" )]
    [PseudoLayerOrder( "A2" )]
    [PseudoLayerOrder( "A3" )]
    [PseudoLayerOrder( "A4" )]
    [PseudoLayerOrder( "A5" )]

    // <target>
    internal class Target
    {
        public static void Foo()
        {
            Console.WriteLine( "This is original code (discarded)." );
        }

        [PseudoIntroduction( "A1" )]
        [PseudoNotInlineable]
        public static void Bar()
        {
            Console.WriteLine( "SHOULD BE DISCARDED (this is introduced code)." );
        }

        [PseudoOverride( nameof(Foo), "A0" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public static void Foo_Override0()
        {
            // Should invoke empty code.
            Link( Static.Target.Bar, Base )();

            // Should invoke empty code.
            Link( Static.Target.Bar, Previous )();

            // Should invoke empty code.
            Link( Static.Target.Bar, Current )();

            // Should invoke the final declaration.
            Link( Static.Target.Bar, Final )();
        }

        [PseudoOverride( nameof(Foo), "A2" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public static void Foo_Override2()
        {
            // Should invoke override 1_2.
            Link( Static.Target.Bar, Base )();

            // Should invoke override 1_2.
            Link( Static.Target.Bar, Previous )();

            // Should invoke override 1_2.
            Link( Static.Target.Bar, Current )();

            // Should invoke the final declaration.
            Link( Static.Target.Bar, Final )();
        }

        [PseudoOverride( nameof(Foo), "A4" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public static void Foo_Override4()
        {
            // Should invoke override 3_2.
            Link( Static.Target.Bar, Base )();

            // Should invoke override 3_2.
            Link( Static.Target.Bar, Previous )();

            // Should invoke override 3_2.
            Link( Static.Target.Bar, Current )();

            // Should invoke the final declaration.
            Link( Static.Target.Bar, Final )();
        }

        [PseudoOverride( nameof(Foo), "A6" )]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public static void Foo_Override6()
        {
            // Should invoke the final declaration.
            Link( Static.Target.Bar, Base )();

            // Should invoke the final declaration.
            Link( Static.Target.Bar, Previous )();

            // Should invoke the final declaration.
            Link( Static.Target.Bar, Current )();

            // Should invoke the final declaration.
            Link( Static.Target.Bar, Final )();
        }

        [PseudoOverride( nameof(Bar), "A1" )]
        [PseudoNotInlineable]
        private static void Bar_Override1_1()
        {
            // Should invoke empty code.
            Link( Static.Target.Bar, Base )();

            // Should invoke empty code.
            Link( Static.Target.Bar, Previous )();

            // Should invoke override 1_2.
            Link( Static.Target.Bar, Current )();

            // Should invoke the final declaration.
            Link( Static.Target.Bar, Final )();
        }

        [PseudoOverride( nameof(Bar), "A1" )]
        [PseudoNotInlineable]
        private static void Bar_Override1_2()
        {
            // Should invoke empty code.
            Link( Static.Target.Bar, Base )();

            // Should invoke override 1_1.
            Link( Static.Target.Bar, Previous )();

            // Should invoke override 1_2.
            Link( Static.Target.Bar, Current )();

            // Should invoke the final declaration.
            Link( Static.Target.Bar, Final )();
        }

        [PseudoOverride( nameof(Bar), "A3" )]
        [PseudoNotInlineable]
        private static void Bar_Override3_1()
        {
            // Should invoke override 1_2.
            Link( Static.Target.Bar, Base )();

            // Should invoke override 1_2.
            Link( Static.Target.Bar, Previous )();

            // Should invoke override 3_2.
            Link( Static.Target.Bar, Current )();

            // Should invoke the final declaration.
            Link( Static.Target.Bar, Final )();
        }

        [PseudoOverride( nameof(Bar), "A3" )]
        [PseudoNotInlineable]
        private static void Bar_Override3_2()
        {
            // Should invoke override 1_2.
            Link( Static.Target.Bar, Base )();

            // Should invoke override 3_1.
            Link( Static.Target.Bar, Previous )();

            // Should invoke override 3_2.
            Link( Static.Target.Bar, Current )();

            // Should invoke the final declaration.
            Link( Static.Target.Bar, Final )();
        }

        [PseudoOverride( nameof(Bar), "A5" )]
        [PseudoNotInlineable]
        private static void Bar_Override5_1()
        {
            // Should invoke override 3_2.
            Link( Static.Target.Bar, Base )();

            // Should invoke override 3_2.
            Link( Static.Target.Bar, Previous )();

            // Should invoke the final declaration.
            Link( Static.Target.Bar, Current )();

            // Should invoke the final declaration.
            Link( Static.Target.Bar, Final )();
        }

        [PseudoOverride( nameof(Bar), "A5" )]
        [PseudoNotInlineable]
        private static void Bar_Override5_2()
        {
            // Should invoke override 3_2.
            Link( Static.Target.Bar, Base )();

            // Should invoke override 5_1.
            Link( Static.Target.Bar, Previous )();

            // Should invoke the final declaration.
            Link( Static.Target.Bar, Current )();

            // Should invoke the final declaration.
            Link( Static.Target.Bar, Final )();
        }
    }
}