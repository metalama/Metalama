// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.ScopeStaticClass
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( Scope = IntroductionScope.Default )]
        public static int DefaultScopeStatic()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce( Scope = IntroductionScope.Static )]
        public int StaticScope()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce( Scope = IntroductionScope.Static )]
        public static int StaticScopeStatic()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce( Scope = IntroductionScope.Target )]
        public int TargetScope()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce( Scope = IntroductionScope.Target )]
        public static int TargetScopeStatic()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }
    }

    // <target>
    [Introduction]
    internal static class TargetClass { }
}