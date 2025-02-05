using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Properties.PartialType_Declarations
{
    /*
     * Tests that overriding properties of types with multiple partial declarations within a single syntax tree does work and targets correct methods.
     */

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var property in builder.Target.Properties)
            {
                builder.With( property ).OverrideAccessors( nameof(Template), nameof(Template), tags: new { name = property.Name } );
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
        public int TargetProperty1
        {
            get
            {
                Console.WriteLine( "This is TargetProperty1." );

                return 42;
            }

            set => Console.WriteLine( "This is TargetProperty1." );
        }
    }

    // <target>
    internal partial class TargetClass
    {
        public int TargetProperty2
        {
            get
            {
                Console.WriteLine( "This is TargetProperty2." );

                return 42;
            }

            set => Console.WriteLine( "This is TargetProperty2." );
        }
    }

    // <target>
    internal partial class TargetClass
    {
        public int TargetProperty3
        {
            get
            {
                Console.WriteLine( "This is TargetProperty3." );

                return 42;
            }

            set => Console.WriteLine( "This is TargetProperty3." );
        }
    }
}