// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @Skipped(Linker test preprocessing does not correctly support conditional access expressions)
#endif

using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TemplateBody.ReturnsInt_ConditionalAccess
{
    // <target>
    internal class Target
    {
        private int Foo( Target? x )
        {
            Console.WriteLine( "Original" );

            return 42;
        }

        [PseudoOverride( nameof(Foo), "TestAspect" )]
        private int? Foo_Override( Target? x )
        {
            Console.WriteLine( "Before" );
            int? result = null;
            result = _local.x?.link( _local.Foo, inline )( this );

            Console.WriteLine( "After" );

            return result;
        }
    }
}