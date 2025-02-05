using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS0067

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.EventFields.ParameterMapping
{
    /*
     * Verifies that template parameter is correctly mapped by index.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Events.Single() )
                .OverrideAccessors(
                    nameof(RenamedValueParameter),
                    nameof(RenamedValueParameter) );
        }

        [Template]
        public void RenamedValueParameter( EventHandler x )
        {
            x.Invoke( null, new EventArgs() );
            meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public event EventHandler? Event;
    }
}