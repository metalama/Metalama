// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.ExistingConflictOverrideBaseNonVirtual
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int BaseMethod()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public static int BaseMethod_Static()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }
    }

    internal class BaseClass
    {
        public int BaseMethod()
        {
            return 13;
        }

        public static int BaseMethod_Static()
        {
            return 13;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass : BaseClass { }
}