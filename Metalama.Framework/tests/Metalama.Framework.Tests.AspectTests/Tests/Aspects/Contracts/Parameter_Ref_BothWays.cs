using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Parameter_Ref_BothWays;

#pragma warning disable CS8618

internal class NotNullAttribute : ContractAspect
{
    protected override ContractDirection GetDefinedDirection( IAspectBuilder builder ) => ContractDirection.Both;

    public override void Validate( dynamic? value )
    {
        if (value == null)
        {
            throw new ArgumentNullException( meta.Target.Parameter.Name );
        }
    }
}

// <target>
internal class Target
{
    private void M( [NotNull] ref string m )
    {
        m = "";
    }
}