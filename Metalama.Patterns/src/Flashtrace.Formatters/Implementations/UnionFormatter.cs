// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Flashtrace.Formatters.Implementations;

/// <summary>
/// The formatter used for a union of C# 15 by default. It formats the type and the value of the case that the union
/// currently carries.
/// </summary>
/// <remarks>
/// <para>
/// The type of the case is part of the output because the value alone does not identify the case. A union of
/// <c>int</c> and <c>long</c> formats the two cases that hold the number one to the same text, and a caller that uses
/// the output as a cache key would then read the entry of one case for the other.
/// </para>
/// <para>
/// Without this formatter a union is formatted by <see cref="DefaultFormatter{TValue}"/>, which calls
/// <see cref="object.ToString"/>. The compiler synthesizes no <c>ToString</c> for a union declaration, so the resolved
/// method is <see cref="System.ValueType.ToString"/> and the output is the name of the union type for every value.
/// </para>
/// </remarks>
internal sealed class UnionFormatter : IFormatter
{
    private readonly Func<object, object?> _getCaseValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnionFormatter"/> class.
    /// </summary>
    /// <param name="repository">The repository from which the formatter of the case value is obtained.</param>
    /// <param name="getCaseValue">The function that reads the value of the current case, obtained from
    /// <see cref="Utilities.UnionReflection.GetCaseValueGetterOrNull"/>.</param>
    public UnionFormatter( IFormatterRepository repository, Func<object, object?> getCaseValue )
    {
        this.Repository = repository ?? throw new ArgumentNullException( nameof(repository) );
        this._getCaseValue = getCaseValue ?? throw new ArgumentNullException( nameof(getCaseValue) );
    }

    /// <inheritdoc />
    public IFormatterRepository Repository { get; }

    /// <inheritdoc />
    public FormatterAttributes Attributes => FormatterAttributes.Normal;

    /// <inheritdoc />
    public void Format( UnsafeStringBuilder stringBuilder, object? value )
    {
        if ( value == null )
        {
            stringBuilder.Append( 'n', 'u', 'l', 'l' );

            return;
        }

        var caseValue = this._getCaseValue( value );

        stringBuilder.Append( '{' );

        if ( caseValue == null )
        {
            stringBuilder.Append( 'n', 'u', 'l', 'l' );
        }
        else
        {
            var caseType = caseValue.GetType();
            this.Repository.Get<Type>().Format( stringBuilder, caseType );
            stringBuilder.Append( ':', ' ' );
            this.Repository.Get( caseType ).Format( stringBuilder, caseValue );
        }

        stringBuilder.Append( '}' );
    }
}
