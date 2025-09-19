// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Inliners.SetterAssignment_NotSimple
{
    // <target>
    internal class Target
    {
        private int _field;

        private int Foo
        {
            get { return 0; }
            set
            {
                Console.WriteLine( "Original");
                this._field = value;
            }
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        private int Foo_Override
        {
            get { return Link[This.Foo.set, Inline]; }
            set
            {
                Console.WriteLine( "Before");
                Link[ This.Foo.set, Inline ] += value;
                Console.WriteLine( "After");
            }
        }
    }
}
