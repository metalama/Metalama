// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using unsafe IntPointer = int*;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.AliasAnyType_Error;

public unsafe class TheAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(M) );
    }

    [CompileTime]
    private void CompileTimeMethod( IntPointer ptr ) { }

    [Template]
    private static void M( IntPointer ptr ) => meta.Proceed();

    [Introduce]
    private static void Introduced( IntPointer ptr ) { }
}

public class C
{
    [TheAspect]
    private static unsafe void M( IntPointer ptr ) { }
}

#endif