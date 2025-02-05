using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.Attributes;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Methods.Attributes
{
    /*
     * Tests that overriding method keeps all the existing attributes.
     */

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods)
            {
                builder.With( method ).Override( nameof(Override) );
            }
        }

        [Template]
        public dynamic? Override()
        {
            Console.WriteLine( "This is the overridden method." );

            return meta.Proceed();
        }
    }

    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        [MethodOnly]
        [return: ReturnValueOnly]
        public void IntroducedMethod<[GenericParameterOnly] T>( [ParameterOnly] int x ) { }
    }

    [AttributeUsage( AttributeTargets.Method )]
    public class MethodOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Parameter )]
    public class ParameterOnly : Attribute { }

    [AttributeUsage( AttributeTargets.ReturnValue )]
    public class ReturnValueOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.GenericParameter )]
    public class GenericParameterOnlyAttribute : Attribute { }

    // <target>
    [Introduction]
    [Override]
    internal class TargetClass
    {
        [MethodOnly]
        [return: ReturnValueOnly]
        public void Method<[GenericParameterOnly] T>( [ParameterOnly] int x ) { }
    }
}