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
/// an <see cref="ArgumentException"/> if the target is assigned a value that
/// is not a valid credit card number. If the target is a nullable type, If the target is a nullable type, null strings are accepted and do not
/// throw an exception.
/// </summary>
/// <seealso href="@contract-types"/>
[PublicAPI]
public sealed class CreditCardAttribute : ContractBaseAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreditCardAttribute"/> class.
    /// </summary>
    public CreditCardAttribute() { }

    /// <inheritdoc/>
    public override void BuildEligibility( IEligibilityBuilder<IFieldOrPropertyOrIndexer> builder )
    {
        base.BuildEligibility( builder );
        builder.Type().MustBeConvertibleTo<string>();
    }

    /// <inheritdoc/>
    public override void BuildEligibility( IEligibilityBuilder<IParameter> builder )
    {
        base.BuildEligibility( builder );
        builder.Type().MustBeConvertibleTo<string>();
    }

    /// <inheritdoc/>
    public override void Validate( dynamic? value )
    {
        if ( !ContractHelpers.IsValidCreditCardNumber( value ) )
        {
            var context = new ContractContext( meta.Target );

            context.Options.Templates!.OnCreditCardContractViolated( value, context );
        }
    }
}