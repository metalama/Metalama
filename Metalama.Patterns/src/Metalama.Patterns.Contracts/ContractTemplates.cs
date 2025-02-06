// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using Metalama.Patterns.Contracts.Numeric;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Provides default implementations for the code templates used by code contract aspects (derived from <see cref="ContractBaseAttribute"/>).
/// This class can be derived and templates can be overridden. To register the new template implementations, use the <see cref="ContractOptions.Templates"/>
/// contract options.
/// </summary>
/// <seealso href="@configuring-contracts"/>
[PublicAPI]
public class ContractTemplates : ITemplateProvider, ICompileTimeSerializable
{
    /// <summary>
    /// Gets the name of the target parameter.
    /// </summary>
    [CompileTime]
    [Obsolete( "Use context.TargetParameterName" )]
    protected static string TargetParameterName => new ContractContext( meta.Target ).TargetParameterName;

    /// <summary>
    /// Gets a human-readable name of the target declaration.
    /// </summary>
    [CompileTime]
    [Obsolete( "Use context.TargetDisplayName" )]
    protected static string TargetDisplayName => new ContractContext( meta.Target ).TargetDisplayName;

    /// <summary>
    /// Template used by the <see cref="CreditCardAttribute"/> contract.
    /// </summary>
    [Template]
    public virtual void OnCreditCardContractViolated( dynamic? value, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentException( $"The {context.TargetDisplayName} must be a valid credit card number.", context.TargetParameterName );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} must be a valid credit card number." );
        }
    }

    /// <summary>
    /// Template used by the <see cref="EnumDataTypeAttribute"/> contract.
    /// </summary>
    [Template]
    public virtual void OnInvalidEnumValue( dynamic? value, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentException(
                $"The {context.TargetDisplayName} must be a valid {context.Type.ToDisplayString()}.",
                context.TargetParameterName );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} must be a valid {context.Type.ToDisplayString()}." );
        }
    }

    /// <summary>
    /// Template used by the <see cref="NotEmptyAttribute"/> contract.
    /// </summary>
    [Template]
    public virtual void OnNotEmptyContractViolated( dynamic? value, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentException(
                $"The {context.TargetDisplayName} must not be null or empty.",
                context.TargetParameterName );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} must not be null or empty." );
        }
    }

    /// <summary>
    /// Template used by the <see cref="NotNullAttribute"/> contract.
    /// </summary>
    [Template]
    public virtual void OnNotNullContractViolated( dynamic? value, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentNullException(
                context.TargetParameterName,
                $"The {context.TargetDisplayName} must not be null." );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} must not be null." );
        }
    }

    /// <summary>
    /// Template used by the <see cref="RegexPatternAttribute"/> contract.
    /// </summary>
    [Template]
    public virtual void OnRegularExpressionContractViolated( dynamic? value, dynamic? pattern, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentException( $"The {context.TargetDisplayName} must match the regular expression '{pattern}'.", context.TargetParameterName );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} must match the regular expression '{pattern}'." );
        }
    }

    /// <summary>
    /// Template used by the <see cref="PhoneAttribute"/> contract.
    /// </summary>
    [Template]
    public virtual void OnPhoneContractViolated( dynamic? value, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentException( $"The {context.TargetDisplayName} must be a valid phone number.", context.TargetParameterName );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} must be a valid phone number." );
        }
    }

    /// <summary>
    /// Template used by the <see cref="EmailAttribute"/> contract.
    /// </summary>
    [Template]
    public virtual void OnEmailAddressContractViolated( dynamic? value, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentException( $"The {context.TargetDisplayName} must be a valid email address.", context.TargetParameterName );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} must be a valid email address." );
        }
    }

    /// <summary>
    /// Template used by the <see cref="UrlAttribute"/> contract.
    /// </summary>
    [Template]
    public virtual void OnUrlContractViolated( dynamic? value, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentException( $"The {context.TargetDisplayName} must be a valid URL.", context.TargetParameterName );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} must be a valid URL." );
        }
    }

    /// <summary>
    /// Template used by the <see cref="StringLengthAttribute"/> contract when only the upper bound is specified.
    /// </summary>
    [Template]
    public virtual void OnStringMaxLengthContractViolated( dynamic? value, int maximumLength, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentException(
                $"The  {context.TargetDisplayName} must be a string with a maximum length of {maximumLength}.",
                context.TargetParameterName );
        }
        else
        {
            throw new PostconditionViolationException( $"The  {context.TargetDisplayName} must be a string with a maximum length of {maximumLength}." );
        }
    }

    /// <summary>
    /// Template used by the <see cref="StringLengthAttribute"/> contract when only the lower bound is specified.
    /// </summary>
    [Template]
    public virtual void OnStringMinLengthContractViolated( dynamic? value, int minimumLength, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentException(
                $"The  {context.TargetDisplayName} must be a string with a minimum length of {minimumLength}.",
                context.TargetParameterName );
        }
        else
        {
            throw new PostconditionViolationException( $"The  {context.TargetDisplayName} must be a string with a minimum length of {minimumLength}." );
        }
    }

    /// <summary>
    /// Template used by the <see cref="StringLengthAttribute"/> contract when both the lower and the upper bounds are specified.
    /// </summary>
    [Template]
    public virtual void OnStringLengthContractViolated( dynamic? value, int minimumLength, int maximumLength, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentException(
                $"The  {context.TargetDisplayName} must be a string with length between {minimumLength} and {maximumLength}.",
                context.TargetParameterName );
        }
        else
        {
            throw new PostconditionViolationException(
                $"The  {context.TargetDisplayName} must be a string with length between {minimumLength} and {maximumLength}." );
        }
    }

    /// <summary>
    /// Template used by the <see cref="RangeAttribute"/> contract.
    /// </summary>
    [Template]
    public virtual void OnRangeContractViolated( dynamic? value, [CompileTime] NumericRange range, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentOutOfRangeException(
                context.TargetParameterName,
                value,
                $"The {context.TargetDisplayName} must be in the range {range}." );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} must be in the range {range}.", value );
        }
    }

    /// <summary>
    /// Template used by the <see cref="GreaterThanAttribute"/> contract.
    /// </summary>
    [Template]
    public virtual void OnGreaterThanOrEqualToContractViolated( dynamic? value, [CompileTime] object minValue, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentOutOfRangeException(
                context.TargetParameterName,
                value,
                $"The {context.TargetDisplayName} must be greater than or equal to {minValue}." );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} must be greater than or equal to {minValue}.", value );
        }
    }

    /// <summary>
    /// Template used by the <see cref="LessThanAttribute"/> contract.
    /// </summary>
    [Template]
    public virtual void OnLessThanOrEqualToContractViolated( dynamic? value, [CompileTime] object maxValue, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentOutOfRangeException(
                context.TargetParameterName,
                value,
                $"The {context.TargetDisplayName} must be less than or equal to {maxValue}." );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} must be less than or equal to {maxValue}.", value );
        }
    }

    /// <summary>
    /// Template used by the <see cref="StrictlyGreaterThanAttribute"/> contract.
    /// </summary>
    [Template]
    public virtual void OnStrictlyGreaterThanContractViolated( dynamic? value, [CompileTime] object minValue, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentOutOfRangeException(
                context.TargetParameterName,
                value,
                $"The {context.TargetDisplayName} must be strictly greater than {minValue}." );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} must be strictly greater than {minValue}.", value );
        }
    }

    /// <summary>
    /// Template used by the <see cref="StrictlyLessThanAttribute"/> contract.
    /// </summary>
    [Template]
    public virtual void OnStrictlyLessThanContractViolated( dynamic? value, [CompileTime] object maxValue, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentOutOfRangeException(
                context.TargetParameterName,
                value,
                $"The {context.TargetDisplayName} must be strictly less than {maxValue}." );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} must be strictly less than {maxValue}.", value );
        }
    }

    /// <summary>
    /// Template used by the <see cref="StrictRangeAttribute"/> contract.
    /// </summary>
    [Template]
    public virtual void OnStrictRangeContractViolated( dynamic? value, [CompileTime] NumericRange range, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentOutOfRangeException(
                context.TargetParameterName,
                value,
                $"The {context.TargetDisplayName} must be strictly in the range {range}." );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} must be strictly  in the range {range}.", value );
        }
    }

    /// <summary>
    /// Template used by the <see cref="RequiredAttribute"/> contract when the value is null.
    /// </summary>
    [Template]
    public virtual void OnRequiredContractViolated( dynamic? value, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentNullException( context.TargetParameterName, $"The {context.TargetDisplayName} is required." );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} is required." );
        }
    }

    /// <summary>
    /// Template used by the <see cref="RequiredAttribute"/> contract when the value is an empty string.
    /// </summary>
    [Template]
    public virtual void OnRequiredContractViolatedBecauseOfEmptyString( dynamic value, ContractContext context )
    {
        if ( context.Direction == ContractDirection.Input )
        {
            throw new ArgumentException( $"The {context.TargetDisplayName} is required.", context.TargetParameterName );
        }
        else
        {
            throw new PostconditionViolationException( $"The {context.TargetDisplayName} is required." );
        }
    }
}