using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.TemplateInStaticClass;

internal class Aspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "regular template" );
        StaticClass.StaticTemplate( 1 );
        meta.InvokeTemplate( nameof(StaticClass.StaticTemplate), TemplateProvider.FromType<StaticClass>(), new { i = 2 } );

        return default;
    }
}

internal class StaticClass : ITemplateProvider
{
    [Template]
    public static void StaticTemplate( [CompileTime] int i )
    {
        Console.WriteLine( $"static template i={i}" );
    }
}

internal class TargetCode
{
    // <target>
    [Aspect]
    private void Method() { }
}