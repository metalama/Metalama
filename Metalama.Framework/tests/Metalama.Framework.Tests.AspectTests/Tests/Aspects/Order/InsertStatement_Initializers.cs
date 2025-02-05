using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Order.InsertStatement_Initializers;

#pragma warning disable CS8618

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Test1Attribute), typeof(OverrideAttribute), typeof(Test2Attribute) )]

/*
 * Tests that multiple contract aspects are ordered correctly, and this order is kept when override is placed in between.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Order.InsertStatement_Initializers;

internal class Test1Attribute : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.AddInitializer( nameof(Template), args: new { order = 1 } );
        builder.AddInitializer( nameof(Template), args: new { order = 2 } );
    }

    [Template]
    public void Template( [CompileTime] int order )
    {
        Console.WriteLine( $"Contract by aspect 1 on {meta.Target.Declaration}, ordinal {order}" );
    }
}

internal class OverrideAttribute : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.Override( nameof(Template) );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( $"Constructor override." );
        meta.Proceed();
    }
}

internal class Test2Attribute : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.AddInitializer( nameof(Template), args: new { order = 1 } );
        builder.AddInitializer( nameof(Template), args: new { order = 2 } );
    }

    [Template]
    public void Template( [CompileTime] int order )
    {
        Console.WriteLine( $"Contract by aspect 2 on {meta.Target.Declaration}, ordinal {order}" );
    }
}

// <target>
internal class Target
{
    [Test1]
    [Test2]
    public Target()
    {
        Console.WriteLine( $"Constructor source (no override)." );
    }

    [Test1]
    [Override]
    [Test2]
    public Target( int o )
    {
        Console.WriteLine( $"Constructor source (with override)." );
    }
}