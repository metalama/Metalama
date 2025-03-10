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

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.RefReadonlyParameter_Invoke;

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var called = builder.IntroduceMethod( nameof(Called) ).Declaration;

        builder.IntroduceMethod( nameof(Caller), args: new { called } );
    }

    [Template]
    private void Called( in int i, ref readonly int j ) { }

    [Template]
    private void Caller( IMethod called, in int i, ref int j, ref readonly int k )
    {
        called.Invoke( meta.Target.Parameters[0].Value, meta.Target.Parameters[0].Value );
        called.Invoke( meta.Target.Parameters[1].Value, meta.Target.Parameters[1].Value );
        called.Invoke( meta.Target.Parameters[2].Value, meta.Target.Parameters[2].Value );
    }
}

[TheAspect]
internal class C { }

#endif