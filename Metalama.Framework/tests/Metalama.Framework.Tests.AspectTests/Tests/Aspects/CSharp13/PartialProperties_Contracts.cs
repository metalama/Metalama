#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
#endif

#if ROSLYN_4_12_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.PartialProperties_Contracts;

public class TheAspect : ContractAspect
{
    public override void Validate(dynamic? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(meta.Target.Declaration.ToString());
        }
    }
}

// <target>
partial class Target
{
    [TheAspect]
    partial string P1 { get; set; }

    partial string P1 { get => "foo"; set => throw new Exception(); }

    partial string P2 { get; set; }

    [TheAspect]
    partial string P2 { get => "foo"; set => throw new Exception(); }

    [TheAspect]
    partial string P3 { get; }

    partial string P3 { get => "foo"; }
}

#endif