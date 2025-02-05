using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Indexer_Introduced;

#pragma warning disable CS8618, CS0169

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(IntroduceAndFilterAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Indexer_Introduced
{
    /*
     * Tests that filter works on introduced property within the same aspect.
     */

    internal class IntroduceAndFilterAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var indexer in builder.Target.Indexers)
            {
                builder.With( indexer ).AddContract( nameof(Filter), ContractDirection.Both );

                foreach (var param in indexer.Parameters)
                {
                    builder.With( param ).AddContract( nameof(Filter) );
                }
            }

            var introducedIndexer = builder.IntroduceIndexer(
                    TypeFactory.GetType( typeof(string) ).ToNullable(),
                    nameof(GetTemplate),
                    nameof(SetTemplate) )
                .Declaration;

            builder.With( introducedIndexer ).AddContract( nameof(Filter), ContractDirection.Both );

            foreach (var param in introducedIndexer.Parameters)
            {
                builder.With( param ).AddContract( nameof(Filter) );
            }
        }

        [Template]
        public void SetTemplate()
        {
            meta.Proceed();
        }

        [Template]
        public string? GetTemplate()
        {
            return meta.Proceed();
        }

        [Template]
        public void Filter( dynamic? value )
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }
        }
    }

    // <target>
    [IntroduceAndFilter]
    internal class Target
    {
        public string? this[ string? x, string? y ]
        {
            get
            {
                return x + y;
            }

            set { }
        }
    }
}