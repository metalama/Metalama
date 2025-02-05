using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.CrossAssembly;
using System;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.CrossAssembly
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public event EventHandler IntroducedEvent
        {
            add
            {
                Console.WriteLine( "Original" );
            }
            remove
            {
                Console.WriteLine( "Original" );
            }
        }
    }

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var @event in builder.Target.Events)
            {
                builder.With( @event ).OverrideAccessors( nameof(Template), nameof(Template) );
            }
        }

        [Template]
        public dynamic? Template()
        {
            Console.WriteLine( "Override" );

            return meta.Proceed();
        }
    }
}