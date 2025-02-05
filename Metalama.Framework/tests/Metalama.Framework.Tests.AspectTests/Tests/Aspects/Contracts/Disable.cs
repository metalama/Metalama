using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Disable;

internal class NotNullAttribute : ContractAspect
{
    protected override ContractDirection GetActualDirection( IAspectBuilder builder, ContractDirection direction ) => ContractDirection.None;

    public override void Validate( dynamic? value )
    {
        if (value == null)
        {
            throw new ArgumentNullException();
        }
    }
}

// <target>
internal class C
{
    [NotNull]
    public string? P { get; set; }
}