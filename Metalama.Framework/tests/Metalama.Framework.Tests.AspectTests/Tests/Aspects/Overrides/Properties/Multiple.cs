using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Multiple;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(FirstOverrideAttribute), typeof(SecondOverrideAttribute), typeof(IntroduceAndOverrideAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Multiple
{
    /*
     * Tests that multiple aspects overriding the same property produce correct code.
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

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "First override." );
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

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "Second override." );
                meta.Proceed();
            }
        }
    }

    public class IntroduceAndOverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var f in builder.AdvisedTarget.FieldsAndProperties.Where( f => !f.IsImplicitlyDeclared ))
            {
                builder.With( f ).AddAspect( new FirstOverrideAttribute() );
                builder.With( f ).AddAspect( new SecondOverrideAttribute() );
            }
        }

        [Introduce]
        public int IntroducedProperty
        {
            get
            {
                return 42;
            }

            set { }
        }

        [Introduce]
        public int IntroducedAutoProperty { get; set; }

        [Introduce]
        public int IntroducedGetOnlyAutoProperty { get; }
    }

    // <target>
    [IntroduceAndOverride]
    internal class TargetClass
    {
        private int _field;

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

        public int ExpressionBodiedProperty => 42;

        public int AutoProperty { get; set; }

        public int GetOnlyAutoProperty { get; }

        public int InitializerAutoProperty { get; set; } = 42;

        public TargetClass()
        {
            GetOnlyAutoProperty = 42;
        }
    }
}