using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Operators.Attributes_Uninlineable
{
    /*
     * Tests that overriding operators with uninlineable template keeps all the existing attributes.
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
            _ = meta.Proceed();

            return meta.Proceed();
        }
    }

    [AttributeUsage( AttributeTargets.Method )]
    public class MethodOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Parameter )]
    public class ParameterOnly : Attribute { }

    [AttributeUsage( AttributeTargets.ReturnValue )]
    public class ReturnValueOnlyAttribute : Attribute { }

    // <target>
    [Override]
    internal class TargetClass
    {
        [MethodOnly]
        [return: ReturnValueOnly]
        public static TargetClass operator +( [ParameterOnly] TargetClass right )
        {
            return right;
        }

        [MethodOnly]
        [return: ReturnValueOnly]
        public static explicit operator int( [ParameterOnly] TargetClass x )
        {
            return 42;
        }
    }
}