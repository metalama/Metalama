// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Custom attribute that, when added to a field, property or parameter, throws
/// an <see cref="ArgumentNullException"/> if the target is assigned a <see langword="null"/> value.
/// </summary>
/// <seealso href="@contract-types"/>
/// <seealso href="@enforcing-non-nullability"/>
[PublicAPI]
public sealed class NotNullAttribute : ContractBaseAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotNullAttribute"/> class.
    /// </summary>
    public NotNullAttribute() { }

    public override void BuildAspect( IAspectBuilder<IParameter> builder )
    {
        base.BuildAspect( builder );

        builder.WarnIfNullable();
    }

    public override void BuildAspect( IAspectBuilder<IFieldOrPropertyOrIndexer> builder )
    {
        base.BuildAspect( builder );

        builder.WarnIfNullable();
    }

    /// <inheritdoc/>
    public override void BuildEligibility( IEligibilityBuilder<IFieldOrPropertyOrIndexer> builder )
    {
        base.BuildEligibility( builder );

        builder.MustSatisfy(
            f => f.Type.IsReferenceType != false || f.Type.IsNullable != false,
            f => $"the type of {f} must be a nullable type" );
    }

    /// <inheritdoc/>
    public override void BuildEligibility( IEligibilityBuilder<IParameter> builder )
    {
        base.BuildEligibility( builder );

        builder.MustSatisfy(
            p => p.Type.IsReferenceType != false || p.Type.IsNullable != false,
            p => $"the type of {p} must be a nullable type" );
    }

    /// <inheritdoc/>
    public override void Validate( dynamic? value )
    {
        if ( value == null! )
        {
            var context = new ContractContext( meta.Target );
            context.Options.Templates!.OnNotNullContractViolated( value, context );
        }
    }
}