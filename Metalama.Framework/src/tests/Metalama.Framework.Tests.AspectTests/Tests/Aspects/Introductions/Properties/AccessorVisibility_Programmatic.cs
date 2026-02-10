// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.AccessorVisibility_Programmatic
{
    /*
     * Tests that accessor accessibility is correctly set when introducing properties
     * with separate accessor templates that have different accessibility levels.
     * Regression test for issue #820.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            // Introduce a property with a public getter and a private setter.
            builder.IntroduceProperty(
                "PublicGetPrivateSet",
                nameof(PublicGetter),
                nameof(PrivateSetter) );

            // Introduce a property with a private getter and a public setter.
            builder.IntroduceProperty(
                "PrivateGetPublicSet",
                nameof(PrivateGetter),
                nameof(PublicSetter) );

            // Introduce a property with a protected getter and a private setter.
            builder.IntroduceProperty(
                "ProtectedGetPrivateSet",
                nameof(ProtectedGetter),
                nameof(PrivateSetter) );
        }

        [Template]
        public int PublicGetter()
        {
            return meta.Proceed();
        }

        [Template]
        private void PrivateSetter( int value )
        {
            meta.Proceed();
        }

        [Template]
        private int PrivateGetter()
        {
            return meta.Proceed();
        }

        [Template]
        public void PublicSetter( int value )
        {
            meta.Proceed();
        }

        [Template]
        protected int ProtectedGetter()
        {
            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}
