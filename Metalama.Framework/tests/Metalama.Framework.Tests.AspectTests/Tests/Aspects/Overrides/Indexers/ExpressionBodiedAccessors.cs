using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Indexers.ExpressionBodiedAccessors
{
    public class TestAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var indexer in builder.Target.Indexers)
            {
                builder.With( indexer ).OverrideAccessors( nameof(GetIndexer), nameof(SetIndexer) );
            }
        }

        [Template]
        public dynamic? GetIndexer()
        {
            Console.WriteLine( "Override" );

            return meta.Proceed();
        }

        [Template]
        public void SetIndexer()
        {
            Console.WriteLine( "Override" );
            meta.Proceed();
        }
    }

    // <target>
    [Test]
    internal class TargetClass
    {
        public int this[ int x ]
        {
            get => x;
            set => Console.WriteLine( "Original" );
        }
    }
}