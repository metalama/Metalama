using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Finalizers.Simple
{
    // Tests single OverrideMethod aspect with trivial template on methods with trivial bodies.

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Finalizer! ).Override( nameof(Template) );
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( "This is the override." );

            return meta.Proceed();
        }
    }

    // <target>
    [Override]
    internal class TargetClass
    {
        ~TargetClass()
        {
            Console.WriteLine( $"This is the original finalizer." );
        }
    }
}