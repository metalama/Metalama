// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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