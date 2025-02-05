using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.PartialType_Declarations
{
    // Tests that overriding methods of types with multiple partial declarations does work and targets correct methods.

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods)
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
        public void TargetMethod1()
        {
            Console.WriteLine( "This is TargetMethod1." );
        }
    }

    // <target>
    internal partial class TargetClass
    {
        public void TargetMethod2()
        {
            Console.WriteLine( "This is TargetMethod2." );
        }
    }

    // <target>
    internal partial class TargetClass
    {
        public void TargetMethod3()
        {
            Console.WriteLine( "This is TargetMethod3." );
        }
    }
}