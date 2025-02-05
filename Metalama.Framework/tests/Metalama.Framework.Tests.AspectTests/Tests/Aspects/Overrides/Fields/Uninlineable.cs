using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable
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
        [Override]
        public int Field;

        [Override]
        public int StaticField;

        [Override]
        public int InitializerField = 42;

        [Override]
        public readonly int ReadOnlyField;

        public TargetClass()
        {
            ReadOnlyField = 42;
        }
    }
}