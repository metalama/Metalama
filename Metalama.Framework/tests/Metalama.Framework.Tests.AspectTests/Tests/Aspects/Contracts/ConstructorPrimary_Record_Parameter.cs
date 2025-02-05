#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.ConstructorPrimary_Record_Parameter;

internal class NotNullAttribute : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        if (value == null)
        {
            throw new ArgumentNullException( meta.Target.Property.Name );
        }
    }
}

// <target>
internal record class Target( [NotNull] string X )
{
    public string Y { get; set; } = X;
}

#endif