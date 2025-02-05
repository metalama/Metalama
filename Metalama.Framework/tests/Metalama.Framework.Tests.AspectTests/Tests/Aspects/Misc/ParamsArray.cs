using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Diagnostics;

namespace Metalama.Framework.Tests.Integration.Tests.Aspects.Misc.ParamsArray;

public class TheAspect : TypeAspect
{
    [Introduce]
    void ParamsArray(params int[] ints) { }

    [Introduce]
    void Usage()
    {
        ParamsArray(1, 2, 3);

        _ = meta.This[1, 2, 3];
    }

    [Template]
    int ArrayIndexerGetter(params int[] ints) => 0;

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        base.BuildAspect(builder);

        builder.IntroduceIndexer(typeof(int[]), nameof(ArrayIndexerGetter), setTemplate: null, buildIndexer: indexerBuilder => indexerBuilder.Parameters[0].IsParams = true);
    }
}

// <target>
[TheAspect]
public class Target
{
}