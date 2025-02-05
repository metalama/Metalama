using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Accessibility
{
    /*
     * Tests that overriding fields of different accessibility retains the correct accessibility.
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
        private int _implicitlyPrivateField;

        [Override]
        private int _privateField;

        [Override]
        private protected int PrivateProtectedField;

        [Override]
        protected int ProtectedField;

        [Override]
        protected internal int ProtectedInternalField;

        [Override]
        internal int InternalField;

        [Override]
        public int PublicField;
    }
}