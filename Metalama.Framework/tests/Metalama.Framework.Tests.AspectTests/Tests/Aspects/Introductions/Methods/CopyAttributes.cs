using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.CopyAttributes
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceMethod( nameof(ProgrammaticMethod), args: new { S = typeof(int), x = 42 } );
        }

        [Introduce]
        [Foo( 1 )]
        [return: Foo( 2 )]
        public void DeclarativeMethod<[Foo( 3 )] T>( [Foo( 4 )] int x )
        {
            Console.WriteLine( "This is introduced method." );
        }

        [Template]
        [Foo( 1 )]
        [return: Foo( 2 )]
        public void ProgrammaticMethod<[CompileTime] S, [Foo( 3 )] T>( [CompileTime] int x, [Foo( 4 )] int y, [Foo( 5 )] int z )
        {
            Console.WriteLine( "This is introduced method." );
        }
    }

    public class FooAttribute : Attribute
    {
        public FooAttribute( int x ) { }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}