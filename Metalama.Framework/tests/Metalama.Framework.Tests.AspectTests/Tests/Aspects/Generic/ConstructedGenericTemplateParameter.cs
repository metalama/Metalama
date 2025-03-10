// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Generic.ConstructedGenericTemplateParameter;

public class Aspect : MethodAspect
{
    public override void BuildAspect(IAspectBuilder<IMethod> builder)
    {
        base.BuildAspect(builder);

        builder.Advice.Override(builder.Target, nameof(Template), new { T = typeof(int) });
    }

    [Template]
    private dynamic Template<[CompileTime] T>(T[] arg)
    {
        Console.WriteLine(arg[0]);

        return meta.Proceed();
    }
}

// <target>
class TargetCode
{
    [Aspect]
    public int M(int[] arg)
    {
        return 0;
    }
}