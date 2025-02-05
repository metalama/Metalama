#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using unsafe IntPointer = int*;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.AliasAnyType_Error2;

public class TheAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(M) );
    }

    [CompileTime]
    private unsafe void CompileTimeMethod( IntPointer ptr ) { }

    [Template]
    private static unsafe void M( IntPointer ptr ) => meta.Proceed();

    [Introduce]
    private static unsafe void Introduced( IntPointer ptr ) { }
}

public class C
{
    [TheAspect]
    private static unsafe void M( IntPointer ptr ) { }
}

#endif