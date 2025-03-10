// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Fields.Linking.Source_NoPromotion
{
    [PseudoLayerOrder("A0")]
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

        [PseudoOverride(nameof(Foo), "A0")]
        [PseudoNotInlineable]
        [PseudoNotDiscardable]
        public int Foo_Override0
        {
            get
            {
                // Should invoke the final declaration.
                _ = link(_this.Bar, @base);
                // Should invoke the final declaration.
                _ = link(_this.Bar, previous);
                // Should invoke the final declaration.
                _ = link(_this.Bar, current);
                // Should invoke the final declaration.
                _ = link(_this.Bar, final);

                return 42;
            }
            set
            {
                // Should invoke the final declaration.
                link[_this.Bar, @base] = value;
                // Should invoke the final declaration.
                link[_this.Bar, previous] = value;
                // Should invoke the final declaration.
                link[_this.Bar, current] = value;
                // Should invoke the final declaration.
                link[_this.Bar, final] = value;
            }
        }
    }
}