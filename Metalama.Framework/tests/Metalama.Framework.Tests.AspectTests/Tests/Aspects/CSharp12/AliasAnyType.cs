#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using MyTuple = (int, int Name);
using unsafe IntPointer = int*;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.AliasAnyType;

public class TheAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(M) );
    }

    [CompileTime]
    private void CompileTimeMethod( MyTuple tuple ) { }

    [Template]
    private static void M( MyTuple tuple ) => meta.Proceed();

    [Introduce]
    private static void Introduced( MyTuple tuple ) { }
}

public class C
{
    [TheAspect]
    private static unsafe void M( MyTuple tuple, IntPointer ptr ) { }
}

#endif