// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(DesignTime)
#endif

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.DesignTime
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public void IntroducedMethod_Void()
        {
            Console.WriteLine( "This is introduced method." );
            var nic = meta.Proceed();
        }

        [Introduce]
        public int IntroducedMethod_Int()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }

        [Introduce]
        public int IntroducedMethod_Param( int x )
        {
            Console.WriteLine( $"This is introduced method, x = {x}." );

            return meta.Proceed();
        }

        [Introduce]
        public static int IntroducedMethod_StaticSignature()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }

        [Introduce( IsVirtual = true )]
        public int IntroducedMethod_VirtualExplicit()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal partial class TargetClass { }
}