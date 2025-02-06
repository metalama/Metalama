// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Custom attribute that, when added to a field, property or parameter, throws
/// an <see cref="ArgumentException"/> if the target is assigned a string of invalid length.
/// If the target is a nullable type, If the target is a nullable type, null strings are accepted and do not throw an exception.
/// </summary>
/// <seealso href="@contract-types"/>
[PublicAPI]
public sealed class StringLengthAttribute : ContractBaseAttribute
{
    // TODO: Add diagnostics if the aspect construction is invalid (eg, max < min).

    /// <summary>
    /// Initializes a new instance of the <see cref="StringLengthAttribute"/> class.
    /// </summary>
    /// <param name="maximumLength">Maximum length.</param>
    public StringLengthAttribute( int maximumLength )
    {
        this.MaximumLength = maximumLength;
        this.MinimumLength = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringLengthAttribute"/> class.
    /// </summary>
    /// <param name="maximumLength">Maximum length.</param>
    /// <param name="minimumLength">Minimum length.</param>
    public StringLengthAttribute( int minimumLength, int maximumLength )
    {
        this.MaximumLength = maximumLength;
        this.MinimumLength = minimumLength;
    }

    /// <summary>
    /// Gets the maximum length.
    /// </summary>
    public int MaximumLength { get; }

    /// <summary>
    /// Gets the minimum length.
    /// </summary>
    public int MinimumLength { get; }

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
        // TODO: We assume that min and max are sensible (eg, non-negative) here. This should be validated ideally at compile time. See comment at head of class.

        var context = new ContractContext( meta.Target );
        var targetType = context.Type;
        var requiresNullCheck = targetType.IsNullable != false;

        var templates = context.Options.Templates!;

        if ( this.MinimumLength == 0 && this.MaximumLength != int.MaxValue )
        {
            if ( requiresNullCheck )
            {
                if ( value != null && value!.Length > this.MaximumLength )
                {
                    templates.OnStringMaxLengthContractViolated( value, this.MaximumLength, context );
                }
            }
            else
            {
                if ( value!.Length > this.MaximumLength )
                {
                    templates.OnStringMaxLengthContractViolated( value, this.MaximumLength, context );
                }
            }
        }
        else if ( this.MinimumLength > 0 && this.MaximumLength == int.MaxValue )
        {
            if ( requiresNullCheck )
            {
                if ( value != null && value!.Length < this.MinimumLength )
                {
                    templates.OnStringMinLengthContractViolated( value, this.MinimumLength, context );
                }
            }
            else
            {
                if ( value!.Length < this.MinimumLength )
                {
                    templates.OnStringMinLengthContractViolated( value, this.MinimumLength, context );
                }
            }
        }
        else if ( this.MinimumLength > 0 && this.MaximumLength != int.MaxValue )
        {
            if ( requiresNullCheck )
            {
                if ( value != null && (value!.Length < this.MinimumLength || value.Length > this.MaximumLength) )
                {
                    templates.OnStringLengthContractViolated( value, this.MinimumLength, this.MaximumLength, context );
                }
            }
            else
            {
                if ( value!.Length < this.MinimumLength || value.Length > this.MaximumLength )
                {
                    templates.OnStringLengthContractViolated( value, this.MinimumLength, this.MaximumLength, context );
                }
            }
        }

        // else: min is zero, max is int.MaxVal, all strings are valid, no need to check.
    }
}