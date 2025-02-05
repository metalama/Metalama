using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Indexers.CrossAssembly;
using System;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Indexers.CrossAssembly
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceIndexer( typeof(int), nameof(Template), nameof(Template) );
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
            foreach (var indexer in builder.Target.Indexers)
            {
                builder.With( indexer ).OverrideAccessors( nameof(Template), nameof(Template) );
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