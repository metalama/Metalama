#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.NameofInstanceFromStatic;

#pragma warning disable CS0649 // Field is never assigned

public class TheAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(M) );
    }

    private string? p;

    [Template]
    private static string M() => meta.Proceed() + nameof(p.Length);
}

public class C
{
    private string? p;

    [TheAspect]
    private static string M() => nameof(p.Length);
}

#endif