using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Indexers.Attributes
{
    /*
     * Tests that overriding indexers of keeps all the existing attributes.
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

    [AttributeUsage( AttributeTargets.Property )]
    public class PropertyOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Method )]
    public class MethodOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.ReturnValue )]
    public class ReturnValueOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Parameter )]
    public class ParamOnlyAttribute : Attribute { }

    // <target>
    [Override]
    internal class TargetClass
    {
        [PropertyOnly]
        public int this[ [ParamOnly] int x ]
        {
            [return: ReturnValueOnly]
            [method: MethodOnly]
            get
            {
                Console.WriteLine( "Original Property" );

                return 42;
            }

            [return: ReturnValueOnly]
            [method: MethodOnly]
            [param: ParamOnly]
            set
            {
                Console.WriteLine( "Original Property" );
            }
        }
    }
}