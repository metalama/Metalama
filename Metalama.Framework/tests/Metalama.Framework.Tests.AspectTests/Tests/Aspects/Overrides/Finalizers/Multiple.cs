using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Finalizers.Multiple;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(FirstOverrideAttribute), typeof(SecondOverrideAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Finalizers.Multiple
{
    // Tests single OverrideMethod aspect with trivial template on methods with trivial bodies.

    public class FirstOverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Finalizer! ).Override( nameof(Template) );
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( "This is the first override." );

            return meta.Proceed();
        }
    }

    public class SecondOverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Finalizer! ).Override( nameof(Template) );
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( "This is the second override." );

            return meta.Proceed();
        }
    }

    // <target>
    [FirstOverride]
    [SecondOverride]
    internal class TargetClass
    {
        ~TargetClass()
        {
            Console.WriteLine( $"This is the original finalizer." );
        }
    }
}