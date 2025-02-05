using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Indexers.CopyAttributes
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceIndexer(
                new[] { ( typeof(int), "y" ), ( typeof(int), "z" ) },
                nameof(GetTemplate),
                nameof(SetTemplate),
                args: new { x = 42 },
                buildIndexer: i => i.Type = TypeFactory.GetType( typeof(int) ) );
        }

        [Template]
        [Foo( 1 )]
        [return: Foo( 2 )]
        public dynamic? GetTemplate( [CompileTime] int x, [Foo( 3 )] dynamic? y, [Foo( 4 )] dynamic? z )
        {
            return x + y + z;
        }

        [Template]
        [Foo( 1 )]
        [return: Foo( 2 )]
        public void SetTemplate( [CompileTime] int x, [Foo( 3 )] dynamic? y, [Foo( 4 )] dynamic? z )
        {
            var w = x + y + z;
        }
    }

    public class FooAttribute : Attribute
    {
        public FooAttribute( int z ) { }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}