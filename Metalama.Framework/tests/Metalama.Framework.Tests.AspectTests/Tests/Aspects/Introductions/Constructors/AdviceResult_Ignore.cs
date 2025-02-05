using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;

#pragma warning disable CS0618 // IAdviceResult.AspectBuilder is obsolete

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Constructors.AdviceResult_Ignore;

public class TestAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var result = builder.IntroduceConstructor( nameof(ConstructorTemplate), whenExists: OverrideStrategy.Ignore );

        if (result.Outcome != AdviceOutcome.Ignore)
        {
            throw new InvalidOperationException( $"Outcome was {result.Outcome} instead of Ignored." );
        }

        if (result.AdviceKind != AdviceKind.IntroduceConstructor)
        {
            throw new InvalidOperationException( $"AdviceKind was {result.AdviceKind} instead of IntroduceMethod." );
        }

        if (result.Declaration != builder.Target.Constructors.Single().ForCompilation( result.Declaration.Compilation ))
        {
            throw new InvalidOperationException( $"Declaration was not correct." );
        }
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