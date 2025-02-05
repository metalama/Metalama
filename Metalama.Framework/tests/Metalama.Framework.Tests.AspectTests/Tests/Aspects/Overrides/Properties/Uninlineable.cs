using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Uninlineable
{
    /*
     * Tests that override with an uninlineable expanded template produces correct code.
     */

    public class OverrideAttribute : FieldOrPropertyAspect
    {
        public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
        {
            builder.Override( nameof(Template) );
        }

        [Template]
        public dynamic? Template
        {
            get
            {
                Console.WriteLine( "Override." );
                _ = meta.Proceed();

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "Override." );
                meta.Proceed();
                meta.Proceed();
            }
        }
    }

    // <target>
    internal class TargetClass
    {
        private int _field;

        [Override]
        public int Property
        {
            get
            {
                return _field;
            }

            set
            {
                _field = value;
            }
        }

        private static int _staticField;

        [Override]
        public static int StaticProperty
        {
            get
            {
                return _staticField;
            }

            set
            {
                _staticField = value;
            }
        }

        [Override]
        public int ExpressionBodiedProperty => 42;

        [Override]
        public int AutoProperty { get; set; }

        [Override]
        public int AutoGetOnlyProperty { get; }
    }
}