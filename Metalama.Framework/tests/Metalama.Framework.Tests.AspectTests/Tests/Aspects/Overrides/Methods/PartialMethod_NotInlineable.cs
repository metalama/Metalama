using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.PartialMethod_NotInlineable
{
    /*
     * Tests that overriding partial methods does work when the advice is not inlineable.
     */

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
            _ = meta.Proceed();

            return meta.Proceed();
        }
    }

    // <target>
    [Override]
    internal partial class TargetClass
    {
        public partial int TargetMethod();

        partial void TargetVoidMethodNoImplementation();

        partial void TargetVoidMethodWithImplementation();
    }

    // <target>
    internal partial class TargetClass
    {
        public partial int TargetMethod()
        {
            Console.WriteLine( "This is a partial method." );

            return 42;
        }

        partial void TargetVoidMethodWithImplementation()
        {
            Console.WriteLine( "This is a partial method." );
        }
    }
}