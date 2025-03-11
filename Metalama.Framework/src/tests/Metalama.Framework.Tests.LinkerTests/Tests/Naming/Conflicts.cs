// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0067
#pragma warning disable CS0649

namespace Metalama.Framework.Tests.LinkerTests.Tests.Naming.Conflicts
{    
    // <target>
    class Target
    {
        public int Foo { get; set; }

        [PseudoOverride(nameof(Foo), "TestAspect")]
        public int Foo_Override
        {
            get
            {
                return link[_this.Foo.set];
            }
            set
            {
                link[_this.Foo.set] = value;
            }
        }

        public int _foo { get; set; }

        public int Foo_Source { get; set; }

        public int _foo1()
        {
            return 42;
        }

        public int Foo_Source1()
        {
            return 42;
        }

        public event EventHandler? _foo2;

        public event EventHandler? Foo_Source2;

        public int _foo3;

        public int Foo_Source3;
    }   
}
