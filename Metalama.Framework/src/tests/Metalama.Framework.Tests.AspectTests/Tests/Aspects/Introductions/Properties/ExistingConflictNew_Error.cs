// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.ExistingConflictNew_Error
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
        public int ExistingProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static int ExistingProperty_Static
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int ExistingVirtualProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public int ExistingProperty
        {
            get => 27;
        }

        public static int ExistingProperty_Static
        {
            get => 27;
        }

        public virtual int ExistingVirtualProperty
        {
            get => 27;
        }
    }
}