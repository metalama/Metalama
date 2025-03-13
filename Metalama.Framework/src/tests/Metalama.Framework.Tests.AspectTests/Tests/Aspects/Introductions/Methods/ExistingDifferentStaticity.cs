// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.ExistingDifferentStaticity
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public static int ExistingMethod()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce]
        public int ExistingMethod_Static()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public int ExistingMethod()
        {
            return 0;
        }

        public static int ExistingMethod_Static()
        {
            return 0;
        }
    }
}