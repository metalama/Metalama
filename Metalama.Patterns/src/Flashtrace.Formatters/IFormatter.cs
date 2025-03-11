// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Flashtrace.Formatters;

/// <summary>
/// Appends the description of an object into an <see cref="UnsafeStringBuilder"/>. Weakly-typed variant of <see cref="IFormatter{T}"/>.
/// </summary>
[PublicAPI]
public interface IFormatter
{
    /// <summary>
    /// Gets the <see cref="IFormatterRepository"/> which current <see cref="IFormatter"/> uses to get formatters for other types.
    /// </summary>
    IFormatterRepository Repository { get; }

    /// <summary>
    /// Appends the description of an object into given <see cref="UnsafeStringBuilder"/> (weakly-typed variant).
    /// </summary>
    /// <param name="stringBuilder">The target <see cref="UnsafeStringBuilder"/>.</param>
    /// <param name="value">The value to be formatted.</param>
    void Format( UnsafeStringBuilder stringBuilder, object? value );

    /// <summary>
    /// Gets the formatter attributes.
    /// </summary>
    FormatterAttributes Attributes { get; }
}