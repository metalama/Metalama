// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Custom attribute that, when added to a field, property or parameter, throws
/// an <see cref="ArgumentException"/> if the target is assigned a value that
/// is not a valid URL starting with <c>http://</c>, <c>https://</c> or <c>ftp://</c>.
/// If the target is a nullable type, If the target is a nullable type, null strings are accepted and do not
/// throw an exception.
/// </summary>
/// <seealso href="@contract-types"/>
[PublicAPI]
public sealed class UrlAttribute : RegularExpressionBaseAttribute
{
    protected override void OnContractViolated( dynamic? value, dynamic regex, ContractContext context )
    {
        context.Options.Templates!.OnUrlContractViolated( value, context );
    }

    protected override IExpression GetRegex()
    {
        var builder = new ExpressionBuilder();
        builder.AppendTypeName( typeof(ContractHelpers) );
        builder.AppendVerbatim( "." );
        builder.AppendVerbatim( nameof(ContractHelpers.UrlRegex) );

        return builder.ToExpression();
    }
}