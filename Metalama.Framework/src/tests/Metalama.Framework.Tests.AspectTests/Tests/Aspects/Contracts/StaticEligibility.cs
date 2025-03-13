// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.StaticEligibility;

internal class NotNullAttribute : ContractAspect
{
    protected override ContractDirection GetDefinedDirection( IAspectBuilder builder ) => ContractDirection.Input;

    public override void BuildEligibility( IEligibilityBuilder<IParameter> builder ) => BuildEligibilityForDirection( builder, ContractDirection.Input );

    public override void BuildEligibility( IEligibilityBuilder<IFieldOrPropertyOrIndexer> builder )
        => BuildEligibilityForDirection( builder, ContractDirection.Input );

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
    private void M( [NotNull] out string x )
    {
        x = "";
    }

    [NotNull]
    private string P => "";
}