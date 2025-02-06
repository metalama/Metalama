// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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