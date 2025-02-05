using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.Template_CrossAssembly
{
    public class TestAspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods)
            {
                builder.With( method ).Override( nameof(Override) );
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