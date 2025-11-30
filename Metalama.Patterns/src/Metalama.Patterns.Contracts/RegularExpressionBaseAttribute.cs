// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System.Text.RegularExpressions;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Base class for contract aspects that validate strings against regular expressions.
/// </summary>
/// <remarks>
/// <para>Derived classes must implement <see cref="GetRegex"/> to provide the regular expression used for validation.
/// If the target is a nullable type, null strings are accepted and do not throw an exception.</para>
/// <para>To customize the behavior when validation fails, override the <see cref="OnContractViolated"/> template method.</para>
/// </remarks>
/// <seealso cref="RegularExpressionAttribute"/>
/// <seealso cref="EmailAttribute"/>
/// <seealso cref="PhoneAttribute"/>
/// <seealso cref="UrlAttribute"/>
/// <seealso href="@contract-types"/>
[PublicAPI]
public abstract class RegularExpressionBaseAttribute : ContractBaseAttribute
{
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

    /// <summary>
    /// When implemented in a derived class, returns an expression that evaluates to the <see cref="Regex"/>
    /// used for validation.
    /// </summary>
    /// <returns>An expression that evaluates to the regular expression to match against.</returns>
    protected abstract IExpression GetRegex();

    /// <inheritdoc/>
    public override void Validate( dynamic? value )
    {
        var regex = (Regex) this.GetRegex().Value!;
        var context = new ContractContext( meta.Target );

        var targetType = context.Type;
        var requiresNullCheck = targetType.IsNullable != false;

        if ( requiresNullCheck )
        {
            if ( value != null && !regex.IsMatch( (string) value! ) )
            {
                this.OnContractViolated( value, regex, context );
            }
        }
        else
        {
            if ( !regex.IsMatch( (string) value! ) )
            {
                this.OnContractViolated( value, regex, context );
            }
        }
    }

    /// <summary>
    /// Template method called when the contract validation fails. Override to customize the error behavior.
    /// </summary>
    /// <param name="value">The value that failed validation.</param>
    /// <param name="regex">The regular expression that was not matched.</param>
    /// <param name="context">The contract context.</param>
    [Template]
    protected virtual void OnContractViolated( dynamic? value, dynamic regex, ContractContext context )
    {
        context.Options.Templates!.OnRegularExpressionContractViolated( value, regex, context );
    }
}