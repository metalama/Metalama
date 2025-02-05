using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Constructors.CopyAttributes;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor( nameof(Constructor), args: new { S = typeof(int), x = 42 } );
    }

    [Template]
    [Foo( 1 )]
    public void Constructor<[CompileTime] S>( [CompileTime] int x, [Foo( 2 )] int y, [Foo( 3 )] int z )
    {
        Console.WriteLine( "This is introduced constructor." );
    }
}

public class FooAttribute : Attribute
{
    public FooAttribute( int x ) { }
}

// <target>
[Introduction]
internal class TargetClass { }