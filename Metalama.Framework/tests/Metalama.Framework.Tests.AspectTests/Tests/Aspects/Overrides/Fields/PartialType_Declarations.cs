using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.PartialType_Declarations
{
    // Tests that overriding methods of types with multiple partial declarations does work and targets correct methods.

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var field in builder.Target.Fields)
            {
                builder.With( field ).OverrideAccessors( nameof(Template), nameof(Template), tags: new { name = field.Name } );
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
        public int TargetField1;
    }

    // <target>
    internal partial class TargetClass
    {
        public int TargetField2;
    }

    // <target>
    internal partial class TargetClass
    {
        public int TargetField3;
    }
}