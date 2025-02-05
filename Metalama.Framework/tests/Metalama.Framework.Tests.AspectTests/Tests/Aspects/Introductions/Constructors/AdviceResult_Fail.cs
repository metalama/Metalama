using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Constructors.AdviceResult_Fail;

public class TestAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var result = builder.IntroduceConstructor( nameof(ConstructorTemplate), whenExists: OverrideStrategy.Fail );

        if (result.Outcome != AdviceOutcome.Error)
        {
            throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Error." );
        }

        if (result.AdviceKind != AdviceKind.IntroduceConstructor)
        {
            throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceMethod." );
        }

        // TODO: #33060
        //if (result.Declaration != builder.Target.Events.Single())
        //{
        //    throw new InvalidOperationException($"Declaration was not correct.");
        //}
    }

    [Template]
    public void ConstructorTemplate()
    {
        Console.WriteLine( "Aspect code." );
        meta.Proceed();
    }
}

// <target>
[TestAspect]
public class TargetClass
{
    public TargetClass()
    {
        Console.WriteLine( "Source code." );
    }
}