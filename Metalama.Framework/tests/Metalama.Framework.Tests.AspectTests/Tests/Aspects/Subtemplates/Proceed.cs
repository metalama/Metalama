using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.Proceed;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        throw new NotImplementedException();
    }

    public override async Task<dynamic?> OverrideAsyncMethod()
    {
        await Task.Yield();
        Console.WriteLine( "regular template" );

        if (meta.Target.Parameters[0].Value)
        {
            StaticClass.StaticTemplate( 1 );
        }
        else
        {
            meta.InvokeTemplate( nameof(StaticClass.StaticTemplate), TemplateProvider.FromType<StaticClass>(), new { i = 2 } );
        }

        throw new Exception();
    }
}

internal class StaticClass : ITemplateProvider
{
    [Template]
    public static void StaticTemplate( [CompileTime] int i )
    {
        Console.WriteLine( $"static template i={i}" );
        meta.Return( meta.Proceed() );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private async Task Method( bool condition )
    {
        await Task.Yield();
    }
}