// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp12.RefReadonlyParameter_OverrideBase;

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.IntroduceMethod( nameof(M), whenExists: OverrideStrategy.Override );

        builder.IntroduceIndexer(
            new[] { ( typeof(int), "i" ), ( typeof(int), "j" ) },
            nameof(M),
            setTemplate: null,
            whenExists: OverrideStrategy.Override,
            buildIndexer: indexerBuilder =>
            {
                indexerBuilder.Parameters[0].RefKind = RefKind.In;
                indexerBuilder.Parameters[1].RefKind = RefKind.RefReadOnly;
            } );
    }

    [Template]
    protected int M( in int i, ref readonly int j )
    {
        foreach (var parameter in meta.Target.Parameters)
        {
            Console.WriteLine( $"{parameter}: Kind={parameter.RefKind}, Value={parameter.Value}" );
        }

        return meta.Proceed();
    }
}

internal class B
{
    protected virtual int M( in int i, ref readonly int j )
    {
        return i + j;
    }

    protected virtual int this[ in int i, ref readonly int j ] => 42;
}

[TheAspect]
internal class D : B { }

#endif