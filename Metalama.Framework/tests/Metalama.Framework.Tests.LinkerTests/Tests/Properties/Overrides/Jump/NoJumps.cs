// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Properties.Overrides.Jump.NoJumps
{
    // <target>
    class Target
    {
        int _foo;
        int Foo
        {
            get
            {
                return _foo;
            }

            set
            {
                _foo = value;
            }
        }

        [PseudoOverride( nameof(Foo),"TestAspect1")]
        int Foo_Override1
        {
            get
            {
                Console.WriteLine( "Get1");
                int foo;
                foo = link( _this.Foo.get, inline);
                if (foo > 0)
                {
                    return foo;
                }
                else
                {
                    return -foo;
                }
            }
            set
            {
                Console.WriteLine( "Set1");
                if (value != 0)
                {
                    link[ _this.Foo.set, inline ] = value;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        [PseudoOverride( nameof(Foo),"TestAspect2")]
        int Foo_Override2
        {
            get
            {
                Console.WriteLine( "Get2");
                return link[ _this.Foo.get, inline ];
            }
            set
            {
                Console.WriteLine( "Set2");
                link[ _this.Foo.set, inline ] = value;
            }
        }
    }
}
