using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.CrossAssembly_AsyncEnumerable;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.CrossAssembly_AsyncEnumerable
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public async IAsyncEnumerable<int> IntroducedMethod_AsyncIterator()
        {
            Console.WriteLine( "Introduced" );
            await Task.Yield();

            yield return 42;
        }
    }

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods)
            {
                builder.With( method ).Override( nameof(Template) );
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