using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Properties.CopyAttributes
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceProperty(
                "IntroducedProperty",
                nameof(GetTemplate),
                nameof(SetTemplate),
                args: new { x = 42 } );
        }

        [Introduce]
        [Foo( 1 )]
        [field: Foo( 2 )]
        public int IntroducedProperty_Auto
        {
            [return: Foo( 3 )]
            [method: Foo( 4 )]
            get;
            [return: Foo( 5 )]
            [method: Foo( 6 )]
            [param: Foo( 7 )]
            set;
        }

        [Introduce]
        [Foo( 1 )]
        public int IntroducedProperty_Accessors
        {
            [return: Foo( 2 )]
            [method: Foo( 3 )]
            get
            {
                Console.WriteLine( "Get" );

                return 42;
            }

            [return: Foo( 4 )]
            [method: Foo( 5 )]
            [param: Foo( 6 )]
            set
            {
                Console.WriteLine( value );
            }
        }

        [Foo( 1 )]
        [return: Foo( 2 )]
        [Template]
        public int GetTemplate( [CompileTime] int x )
        {
            return x;
        }

        [Foo( 1 )]
        [return: Foo( 2 )]
        [Template]
        public void SetTemplate( [CompileTime] int x, [Foo( 3 )] int y )
        {
            var w = x + y;
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