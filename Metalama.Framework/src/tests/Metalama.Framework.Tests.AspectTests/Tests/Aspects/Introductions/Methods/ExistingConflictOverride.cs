// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.ExistingConflictOverride
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int ExistingBaseMethod()
        {
            meta.InsertComment( "Call the base method of the same name" );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public void ExistingBaseMethod_Void()
        {
            meta.InsertComment( "Call the base method of the same name" );
            meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int ExistingMethod()
        {
            meta.InsertComment( "Return a constant" );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public void ExistingMethod_Void()
        {
            meta.InsertComment( "Do nothing." );
            meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int NotExistingMethod()
        {
            meta.InsertComment( "Return default value" );

            return meta.Proceed();
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public void NotExistingMethod_Void()
        {
            meta.InsertComment( "Do nothing" );
            meta.Proceed();
        }
    }

    internal class BaseClass
    {
        public virtual int ExistingBaseMethod()
        {
            return 27;
        }

        public virtual void ExistingBaseMethod_Void() { }
    }

    // <target>
    [Introduction]
    internal class TargetClass : BaseClass
    {
        public int ExistingMethod()
        {
            return 27;
        }

        public void ExistingMethod_Void() { }
    }
}