// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

// ReSharper disable once CheckNamespace
namespace Metalama.Patterns.Contracts;

/// <summary>
/// An example of a contract aspect which mutates the value being validated.
/// </summary>
internal sealed class TrimAttribute : ContractAspect
{
    public override void BuildEligibility( IEligibilityBuilder<IFieldOrPropertyOrIndexer> builder )
    {
        builder.Type().MustEqual( typeof(string) );
        base.BuildEligibility( builder );
    }

    public override void BuildEligibility( IEligibilityBuilder<IParameter> builder )
    {
        builder.Type().MustEqual( typeof(string) );
        base.BuildEligibility( builder );
    }

    public override void Validate( dynamic? value )
    {
        // ReSharper disable once RedundantAssignment
        value = value?.Trim();
    }
}