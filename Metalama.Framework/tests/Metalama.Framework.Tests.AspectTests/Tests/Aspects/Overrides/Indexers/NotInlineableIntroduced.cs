using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Indexers.NotInlineableIntroduced;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Indexers.NotInlineableIntroduced
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceIndexer(
                new[] { ( typeof(int), "x" ) },
                nameof(GetIndexerTemplate),
                nameof(SetIndexerTemplate),
                buildIndexer: p => { p.Accessibility = Accessibility.Public; } );

            builder.IntroduceIndexer(
                new[] { ( typeof(string), "x" ) },
                nameof(GetIndexerTemplate),
                nameof(SetIndexerTemplate),
                buildIndexer: p => { p.Accessibility = Accessibility.Public; } );
        }

        [Template]
        public dynamic? GetIndexerTemplate()
        {
            Console.WriteLine( "Introduced" );

            return meta.Proceed();
        }

        [Template]
        public void SetIndexerTemplate( dynamic? value )
        {
            Console.WriteLine( "Introduced" );
            meta.Proceed();
        }
    }

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var indexer in builder.Target.Indexers)
            {
                builder.With( indexer ).OverrideAccessors( nameof(GetIndexer), nameof(SetIndexer), tags: new { T = "first" } );
                builder.With( indexer ).OverrideAccessors( nameof(GetIndexer), nameof(SetIndexer), tags: new { T = "second" } );
                builder.With( indexer ).OverrideAccessors( nameof(GetIndexer), nameof(SetIndexer), tags: new { T = "third" } );
                builder.With( indexer ).OverrideAccessors( nameof(GetIndexer), nameof(SetIndexer), tags: new { T = "fourth" } );
            }
        }

        [Template]
        public dynamic? GetIndexer()
        {
            Console.WriteLine( $"Override {meta.Tags["T"]}" );
            var x = meta.Proceed();

            return meta.Proceed();
        }

        [Template]
        public void SetIndexer()
        {
            Console.WriteLine( $"Override {meta.Tags["T"]}" );
            meta.Proceed();
            meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    [Override]
    internal class TargetClass { }
}