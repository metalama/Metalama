// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @RequiredConstant(ROSLYN_5_11_0_OR_GREATER)
#endif

// The target body and the override body each declare a labeled loop of the same name, and each leaves its own loop
// with a labeled break and skips an iteration of it with a labeled continue. The inlining splices the target body
// into the override, so the label of the inlined body is renamed. The name of the break and of the continue has to
// follow the rename, because dropping it or leaving it unchanged retargets the jump to another loop without any
// diagnostic. See issue #1947.
//
// The project compiles its own test sources at the language version of LangMaxVersion, which is 14.0 and does not
// accept a labeled break. The project file therefore removes this file from the compilation. The test framework
// reads the file from the source directory, so the test still runs.

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.Labels.LabeledJump
{
    // <target>
    internal class Target
    {
        private void Foo( int x )
        {
        outer:

            for ( var i = 0; i < 2; i++ )
            {
                for ( var j = 0; j < 2; j++ )
                {
                    if ( j == x )
                    {
                        continue outer;
                    }

                    Console.WriteLine( $"Original {i} {j}" );

                    if ( i == x )
                    {
                        break outer;
                    }
                }
            }
        }

        [PseudoOverride( nameof(Foo), "TestAspect" )]
        private void Foo_Override( int x )
        {
        outer:

            for ( var i = 0; i < 2; i++ )
            {
                for ( var j = 0; j < 2; j++ )
                {
                    if ( j == x )
                    {
                        continue outer;
                    }

                    Console.WriteLine( $"Aspect {i} {j}" );

                    if ( i == x )
                    {
                        break outer;
                    }
                }
            }

            Link( This.Foo, Inline )( x );
        }
    }
}
