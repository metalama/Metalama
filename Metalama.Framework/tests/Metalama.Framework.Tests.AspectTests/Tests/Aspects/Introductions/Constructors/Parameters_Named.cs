using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Constructors.Parameters_Named;

internal class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.IntroduceConstructor( nameof(Template), args: new { b = 1, a = 2 } );
    }

    [Template]
    private void Template( [CompileTime] int a = -1, [CompileTime] int b = -2 )
    {
        Console.WriteLine( $"template a={a} b={b}" );
    }
}

// <target>
[Aspect]
internal class TargetCode { }