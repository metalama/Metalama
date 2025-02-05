using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.NoProceed
{
    /*
     * Tests a template without meta.Proceed.
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

                return default;
            }

            set
            {
                Console.WriteLine( "Override." );
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