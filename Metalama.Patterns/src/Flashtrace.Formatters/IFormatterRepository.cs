// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Flashtrace.Formatters;

/// <summary>
/// Accesses a repository of formatters.
/// </summary>
[PublicAPI]
public interface IFormatterRepository
{
    // TODO: Do we still want this generic API? Is it properly implemented for non-sealed types?
    /// <summary>
    /// Gets the <see cref="IFormatter{T}"/> for type <typeparamref name="T"/>.
    /// </summary>
    IFormatter<T> Get<T>();

    /// <summary>
    /// Gets the <see cref="IFormatter"/> for the specified <see cref="Type"/>. 
    /// </summary>
    /// <exception cref="FormatterNotFoundException">The repository cannot provide a formatter for the specified <paramref name="objectType"/>.</exception>
    IFormatter Get( Type objectType );

    /// <summary>
    /// Attempts to get the <see cref="IFormatter"/> for the specified <see cref="Type"/>.
    /// </summary>
    bool TryGet( Type objectType, out IFormatter? formatter );

    /// <summary>
    /// Gets the <see cref="FormattingRole"/> associated with the current formatter repository.
    /// </summary>
    FormattingRole Role { get; }
}