using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.ParametricTemplateProvider;

internal class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var provider = new MyTemplateProvider( new MyContext( 42 ) );

        builder.Advice.WithTemplateProvider( provider )
            .IntroduceMethod( builder.Target, nameof(MyTemplateProvider.NewMethod) );

        builder.Advice.WithTemplateProvider( provider )
            .IntroduceMethod( builder.Target, nameof(MyTemplateProvider.NewMethod2) );
    }
}

internal class MyTemplateProvider : ITemplateProvider
{
    private readonly MyContext _context;

    public MyTemplateProvider( MyContext context )
    {
        _context = context;
    }

    [Template]
    public void NewMethod()
    {
        Console.WriteLine( $"Hello, world {_context.I++}." );
    }

    [Template]
    public void NewMethod2()
    {
        Console.WriteLine( $"Hello, world {_context.I++}." );
    }
}

[CompileTime]
public class MyContext
{
    public int I { get; set; }

    public MyContext( int i )
    {
        I = i;
    }
}

// <target>
[MyAspect]
internal class Target { }