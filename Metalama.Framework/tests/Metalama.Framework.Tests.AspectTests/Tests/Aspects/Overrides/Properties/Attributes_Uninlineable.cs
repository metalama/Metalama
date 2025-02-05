using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes_Uninlineable;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Attributes_Uninlineable
{
    /*
     * Tests that overriding properties with unlineable templates keeps all the existing attributes.
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
                _ = meta.Proceed();

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "This is the overridden setter." );
                meta.Proceed();
                meta.Proceed();
            }
        }
    }

    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
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
            [param: ParamOnly]
            [return: ReturnValueOnly]
            set
            {
                Console.WriteLine( "Original Property" );
                _ = meta.Proceed();
            }
        }
    }

    [AttributeUsage( AttributeTargets.Property )]
    public class PropertyOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Method )]
    public class MethodOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.ReturnValue )]
    public class ReturnValueOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Parameter )]
    public class ParamOnlyAttribute : Attribute { }

    // <target>
    [Introduction]
    [Override]
    internal class TargetClass
    {
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