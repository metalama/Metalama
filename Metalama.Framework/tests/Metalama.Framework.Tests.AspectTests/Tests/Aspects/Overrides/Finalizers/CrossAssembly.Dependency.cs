using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Finalizers.CrossAssembly;
using System;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Finalizers.CrossAssembly
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceFinalizer( nameof(Template) );
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( "Introduced." );

            return meta.Proceed();
        }
    }

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Finalizer! ).Override( nameof(Template) );
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( "Override" );

            return meta.Proceed();
        }
    }
}