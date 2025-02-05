using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Indexers.ExistingDifferentSignature
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceIndexer(
                new[] { ( typeof(int), "x" ), ( typeof(int), "y" ) },
                nameof(ExistingIndexer),
                nameof(ExistingIndexer),
                whenExists: OverrideStrategy.Override,
                buildIndexer: i => { i.Type = TypeFactory.GetType( typeof(int) ); } );
        }

        [Template]
        public dynamic? ExistingIndexer()
        {
            Console.WriteLine( $"This is introduced indexer {meta.Target.Indexer.Parameters[0].Value} {meta.Target.Indexer.Parameters[1].Value}." );

            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public virtual int this[ int x ]
        {
            get
            {
                return 13;
            }

            set { }
        }
    }
}