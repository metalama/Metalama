// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System.Text.RegularExpressions;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Custom attribute that, when added to a field, property or parameter, throws
/// an <see cref="ArgumentException"/> if the target is assigned a value that
/// does not match a given regular expression. If the target is a nullable type, null strings are accepted and do not
/// throw an exception.
/// </summary>
/// <remarks>
/// <para>To create a reusable regular expression contract, derive from <see cref="RegularExpressionBaseAttribute"/>
/// and override <see cref="RegularExpressionBaseAttribute.GetRegex"/>.</para>
/// </remarks>
/// <seealso cref="RegularExpressionBaseAttribute"/>
/// <seealso href="@contract-types"/>
[PublicAPI]
public class RegularExpressionAttribute : RegularExpressionBaseAttribute
{
    /// <summary>
    /// Gets the regular expression pattern to match against.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Gets the options that modify the regular expression.
    /// </summary>
    public RegexOptions Options { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegularExpressionAttribute"/> class.
    /// </summary>
    /// <param name="pattern">The regular expression pattern to match against.</param>
    /// <param name="options">Options that modify the regular expression behavior.</param>
    public RegularExpressionAttribute( string pattern, RegexOptions options = RegexOptions.None )
    {
        this.Pattern = pattern;
        this.Options = options;
    }

    protected override IExpression GetRegex()
    {
        var builder = new ExpressionBuilder();
        builder.AppendTypeName( typeof(ContractHelpers) );
        builder.AppendVerbatim( "." );
        builder.AppendVerbatim( nameof(ContractHelpers.GetRegex) );
        builder.AppendVerbatim( "(" );
        builder.AppendLiteral( this.Pattern );
        builder.AppendVerbatim( ", " );
        builder.AppendLiteral( (int) this.Options );
        builder.AppendVerbatim( ")" );

        return builder.ToExpression();
    }
}