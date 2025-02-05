using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Events.Template_CrossAssembly
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var @event in builder.Target.Events)
            {
                builder.With( @event ).OverrideAccessors( nameof(Override), nameof(Override), null );
            }
        }

        [Template]
        public dynamic? Override()
        {
            Console.WriteLine( "Aspect code" );

            return meta.Proceed();
        }
    }
}