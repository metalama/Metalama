// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.ExistingConflictOverride
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int ExistingBaseProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int ExistingProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public static int ExistingProperty_Static
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int NotExistingProperty
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public static int NotExistingProperty_Static
        {
            get
            {
                Console.WriteLine( "This is introduced property." );

                return meta.Proceed();
            }
        }
    }

    internal class BaseClass
    {
        public virtual int ExistingBaseProperty
        {
            get => 27;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass : BaseClass
    {
        public int ExistingProperty
        {
            get => 27;
        }

        public static int ExistingProperty_Static
        {
            get => 27;
        }
    }
}