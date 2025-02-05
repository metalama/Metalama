using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Accessibility
{
    /*
     * Tests that overriding properties preserves accessibility.
     */

    public class OverrideAttribute : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get
            {
                Console.WriteLine( "This is the overridden getter." );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "This is the overridden setter." );
                meta.Proceed();
            }
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        private int ImplicitlyPrivateProperty { get; set; }

        [Override]
        private int PrivateProperty { get; set; }

        [Override]
        private protected int PrivateProtectedProperty { get; set; }

        [Override]
        protected int ProtectedProperty { get; set; }

        [Override]
        protected internal int ProtectedInternalProperty { get; set; }

        [Override]
        internal int InternalProperty { get; set; }

        [Override]
        public int PublicProperty { get; set; }

        [Override]
        public int RestrictedSetterProperty { get; private set; }

        [Override]
        public int RestrictedInitProperty { get; private init; }

        [Override]
        public int RestrictedGetterProperty { private get; set; }
    }
}