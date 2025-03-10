// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.LocalFunctions.InCompileTimeBlock;

class Aspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        base.BuildAspect(builder);

        builder.IntroduceMethod(nameof(IfTemplate), buildMethod: methodBuilder => methodBuilder.Name = "ConditionFalse", args: new { condition = false });
        builder.IntroduceMethod(nameof(IfTemplate), buildMethod: methodBuilder => methodBuilder.Name = "ConditionTrue", args: new { condition = true });

        builder.IntroduceMethod(nameof(ForeachTemplate), buildMethod: methodBuilder => methodBuilder.Name = "Loop", args: new { n = 3 });
    }

    [Template]
    public void IfTemplate([CompileTime] bool condition)
    {
        if (condition)
        {
            LocalFunction();

            void LocalFunction() { }
        }
    }

    [Template]
    public int ForeachTemplate([CompileTime] int n)
    {
        int sum = 0;

        foreach (var i in Enumerable.Range(0, n))
        {
            sum += LocalFunction();
            int LocalFunction() => i;
        }

        return sum;
    }
}

// <target>
[Aspect]
class Target
{
}