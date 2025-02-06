// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace.Formatters;

/// <summary>
/// Base implementation of the <see cref="IFormatter{T}"/> interface.
/// </summary>
/// <typeparam name="T"></typeparam>
[PublicAPI]
public abstract class Formatter<T> : IFormatter<T>, IOptionAwareFormatter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Formatter{T}"/> class using the specified <see cref="IFormatterRepository"/>
    /// to access formatters for other types.
    /// </summary>
    protected Formatter( IFormatterRepository repository )
    {
        this.Repository = repository ?? throw new ArgumentNullException( nameof(repository) );
    }

    public IFormatterRepository Repository { get; }

    /// <inheritdoc />  
    public void Format( UnsafeStringBuilder stringBuilder, object? value )
    {
        this.Format( stringBuilder, (T?) value );
    }

    /// <inheritdoc />
    public abstract void Format( UnsafeStringBuilder stringBuilder, T? value );

    /// <inheritdoc cref="IOptionAwareFormatter" />
    public virtual IOptionAwareFormatter WithOptions( FormattingOptions options ) => this;

    IOptionAwareFormatter IOptionAwareFormatter.WithOptions( FormattingOptions options ) => this.WithOptions( options );

    /// <inheritdoc />
    public virtual FormatterAttributes Attributes => FormatterAttributes.Normal;
}