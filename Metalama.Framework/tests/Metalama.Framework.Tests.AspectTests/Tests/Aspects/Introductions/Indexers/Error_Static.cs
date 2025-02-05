using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Indexer.Error_Static
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceIndexer(
                new[] { ( typeof(int), "x" ) },
                nameof(GetIndexerTemplate),
                nameof(SetIndexerTemplate),
                buildIndexer: p =>
                {
                    p.Accessibility = Accessibility.Public;
                    p.IsStatic = true;
                } );
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

    // <target>
    [Introduction]
    internal class TargetClass { }
}