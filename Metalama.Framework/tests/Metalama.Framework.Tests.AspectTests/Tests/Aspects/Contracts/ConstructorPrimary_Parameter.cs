#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.ConstructorPrimary_Parameter;

internal class NotNullAttribute : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        if (value == null)
        {
            throw new ArgumentNullException( meta.Target.Parameter.Name );
        }
    }
}

// <target>
internal class Target( [NotNull] string x )
{
    public string X { get; set; } = x;
}

#endif