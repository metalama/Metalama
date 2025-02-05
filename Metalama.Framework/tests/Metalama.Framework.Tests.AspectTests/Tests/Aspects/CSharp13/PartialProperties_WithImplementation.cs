#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_12_0_OR_GREATER)
#endif

#if ROSLYN_4_12_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp13.PartialProperties_WithImplementation;

public class TheAspect : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get
        {
            Console.WriteLine("This is aspect code.");

            return meta.Proceed();
        }
        set
        {
            Console.WriteLine("This is aspect code.");

            meta.Proceed();
        }
    }
}

// <target>
partial class Target
{
    [TheAspect]
    partial int P1 { get; set; }

    partial int P1 { get => 0; set => throw new Exception(); }

    partial int P2 { get; set; }

    [TheAspect]
    partial int P2 { get => 0; set => throw new Exception(); }

    [TheAspect]
    partial int P3 { get; }

    partial int P3 { get => 0; }

}

#endif