#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
#endif

#if ROSLYN_4_12_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Diagnostics;

namespace Metalama.Framework.Tests.Integration.Tests.Aspects.CSharp13.ParamsCollections_Basic;

public class TheAspect : TypeAspect
{
    [Introduce]
    void ParamsArray(params int[] ints) { }

    [Introduce]
    void ParamsSpan(params ReadOnlySpan<int> ints) { }

    [Introduce]
    void Usage()
    {
        ParamsArray(1, 2, 3);
        ParamsSpan(1, 2, 3);

        _ = meta.This[1, 2, 3];
    }

    [Template]
    int ArrayIndexerGetter(params int[] ints) => 0;

    [Template]
    int SpanIndexerGetter(params ReadOnlySpan<int> ints) => 0;

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        base.BuildAspect(builder);

        builder.IntroduceIndexer(typeof(int[]), nameof(ArrayIndexerGetter), setTemplate: null, buildIndexer: indexerBuilder => indexerBuilder.Parameters[0].IsParams = true);

        builder.IntroduceIndexer(typeof(ReadOnlySpan<int>), nameof(SpanIndexerGetter), setTemplate: null, buildIndexer: indexerBuilder => indexerBuilder.Parameters[0].IsParams = true);
    }
}

// <target>
[TheAspect]
public class Target
{
}

#endif