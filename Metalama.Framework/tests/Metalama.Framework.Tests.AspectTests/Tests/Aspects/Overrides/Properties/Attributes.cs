using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes
{
    /*
     * Tests that overriding fields of keeps all the existing attributes.
     */

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var property in builder.Target.Properties)
            {
                builder.With( property ).Override( nameof(Template) );
            }
        }

        [Template]
        public dynamic? Template
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

    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        [field: FieldOnly]
        [field: FieldAndProperty]
        [FieldAndProperty]
        [PropertyOnly]
        public int IntroducedAutoProperty
        {
            [MethodOnly]
            [return: ReturnValueOnly]
            get;
            [MethodOnly]
            [param: ParamOnly]
            [return: ReturnValueOnly]
            set;
        }

        [Introduce]
        [FieldAndProperty]
        [PropertyOnly]
        public int IntroducedProperty
        {
            [MethodOnly]
            [return: ReturnValueOnly]
            get
            {
                Console.WriteLine( "Original Property" );

                return meta.Proceed();
            }

            [MethodOnly]
            [return: ReturnValueOnly]
            [param: ParamOnly]
            set
            {
                Console.WriteLine( "Original Property" );
                _ = meta.Proceed();
            }
        }
    }

    [AttributeUsage( AttributeTargets.Field )]
    public class FieldOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Property )]
    public class PropertyOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Method )]
    public class MethodOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.ReturnValue )]
    public class ReturnValueOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Parameter )]
    public class ParamOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
    public class FieldAndPropertyAttribute : Attribute { }

    // <target>
    [Introduction]
    [Override]
    internal class TargetClass
    {
        [field: FieldOnly]
        [field: FieldAndProperty]
        [FieldAndProperty]
        [PropertyOnly]
        public int AutoProperty
        {
            [MethodOnly]
            [return: ReturnValueOnly]
            get;
            [MethodOnly]
            [param: ParamOnly]
            [return: ReturnValueOnly]
            set;
        }

        [FieldAndProperty]
        [PropertyOnly]
        public int Property
        {
            [MethodOnly]
            [return: ReturnValueOnly]
            get
            {
                Console.WriteLine( "Original Property" );

                return 42;
            }

            [MethodOnly]
            [param: ParamOnly]
            [return: ReturnValueOnly]
            set
            {
                Console.WriteLine( "Original Property" );
            }
        }
    }
}