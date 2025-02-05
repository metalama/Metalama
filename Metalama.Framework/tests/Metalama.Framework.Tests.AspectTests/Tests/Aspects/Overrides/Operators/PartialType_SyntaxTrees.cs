

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.PartialType_SyntaxTrees
{
    /*
     * Tests that overriding operators of types declared in multiple syntax trees does work and targets correct operators.
     */

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods.OfKind( MethodKind.Operator ))
            {
                builder.With( method ).Override( nameof(Template), tags: new { name = method.Name } );
            }
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( $"This is the override of {meta.Tags["name"]}." );

            return meta.Proceed();
        }
    }

    // <target>
    [Override]
    internal partial class TargetClass
    {
        public static TargetClass operator +( TargetClass a, TargetClass b )
        {
            Console.WriteLine( $"This is the original operator." );

            return new TargetClass();
        }
    }
}