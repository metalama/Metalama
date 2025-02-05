#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.DesignTime.AddInitializer;

// This tests that adding initializer is not visible at deisgn time.

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( StatementFactory.Parse( "x = 42;" ), InitializerKind.BeforeInstanceConstructor );
    }
}

// <target>
[Aspect]
internal partial class RegularClass
{
    private int x;
}

// <target>
[Aspect]
internal partial class ClassWithPrimaryConstructor()
{
    private int x;
}

#endif