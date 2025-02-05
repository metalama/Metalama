using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable_Multiple;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(FirstOverrideAttribute), typeof(SecondOverrideAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Uninlineable_Multiple
{
    /*
     * Tests that two overrides with an uninlineable expanded template produces correct code.
     */

    public class FirstOverrideAttribute : FieldOrPropertyAspect
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
                Console.WriteLine( "First override." );
                _ = meta.Proceed();

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "First override." );
                meta.Proceed();
                meta.Proceed();
            }
        }
    }

    public class SecondOverrideAttribute : FieldOrPropertyAspect
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
                Console.WriteLine( "Second override." );
                _ = meta.Proceed();

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "Second override." );
                meta.Proceed();
                meta.Proceed();
            }
        }
    }

    // <target>
    internal class TargetClass
    {
        [FirstOverride]
        [SecondOverride]
        public int Field;

        [FirstOverride]
        [SecondOverride]
        public int StaticField;

        [FirstOverride]
        [SecondOverride]
        public int InitializerField = 42;

        [FirstOverride]
        [SecondOverride]
        public readonly int ReadOnlyField;

        public TargetClass()
        {
            ReadOnlyField = 42;
        }
    }
}