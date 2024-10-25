// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System.Text.RegularExpressions;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// The base class of contracts that are based on custom attributes.
/// </summary>
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

    [Template]
    protected virtual void OnContractViolated( dynamic? value, dynamic regex, ContractContext context )
    {
        context.Options.Templates!.OnRegularExpressionContractViolated( value, regex, context );
    }
}