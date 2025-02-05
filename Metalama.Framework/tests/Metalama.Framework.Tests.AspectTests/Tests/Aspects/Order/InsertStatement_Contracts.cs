using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Order.InsertStatement_Contracts;

#pragma warning disable CS8618

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Test1Attribute), typeof(OverrideAttribute), typeof(Test2Attribute) )]

/*
 * Tests that multiple contract aspects are ordered correctly, and this order is kept when override is placed in between.
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Order.InsertStatement_Contracts;

[AttributeUsage( AttributeTargets.Parameter | AttributeTargets.ReturnValue )]
internal class Test1Attribute : Attribute, IAspect<IParameter>
{
    public void BuildAspect( IAspectBuilder<IParameter> builder )
    {
        var direction = builder.Target.IsReturnParameter ? ContractDirection.Output : ContractDirection.Both;
        builder.AddContract( nameof(Validate), direction, args: new { order = 1 } );
        builder.AddContract( nameof(Validate), direction, args: new { order = 2 } );
    }

    public void BuildEligibility( IEligibilityBuilder<IParameter> builder ) { }

    [Template]
    public void Validate( dynamic? value, [CompileTime] int order )
    {
        Console.WriteLine( $"[Test1] on {value}, ordinal {order}" );
    }
}

internal class OverrideAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"Override." );

        return meta.Proceed();
    }
}

[AttributeUsage( AttributeTargets.Parameter | AttributeTargets.ReturnValue )]
internal class Test2Attribute : Attribute, IAspect<IParameter>
{
    public void BuildAspect( IAspectBuilder<IParameter> builder )
    {
        var direction = builder.Target.IsReturnParameter ? ContractDirection.Output : ContractDirection.Both;
        builder.AddContract( nameof(Validate), direction, args: new { order = 1 } );
        builder.AddContract( nameof(Validate), direction, args: new { order = 2 } );
    }

    public void BuildEligibility( IEligibilityBuilder<IParameter> builder ) { }

    [Template]
    public void Validate( dynamic? value, [CompileTime] int order )
    {
        Console.WriteLine( $"[Test2] on {value}, ordinal {order}" );
    }
}

// <target>
internal class Target
{
    [return: Test1]
    [return: Test2]
    private int NoOverride( [Test1] [Test2] ref int p1, [Test1] [Test2] ref int p2 ) => 42;

    [Override]
    [return: Test1]
    [return: Test2]
    private int Override( [Test1] [Test2] ref int p1, [Test1] [Test2] ref int p2 ) => 42;
}