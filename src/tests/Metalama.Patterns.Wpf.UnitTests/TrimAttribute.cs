// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

// ReSharper disable once CheckNamespace
namespace Metalama.Patterns.Contracts;

// TODO: Remove explicit layers when layer issue is fixed.

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
#pragma warning disable IDE0059 // Unnecessary assignment of a value
        value = value?.Trim();
#pragma warning restore IDE0059 // Unnecessary assignment of a value
    }
}