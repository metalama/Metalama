// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace.Formatters;

/// <summary>
/// Appends the description of an object into an <see cref="UnsafeStringBuilder"/>. Strongly-typed variant of <see cref="IFormatter"/>.
/// </summary>
/// <typeparam name="T">Type of values that can be formatted.</typeparam>
[PublicAPI]
public interface IFormatter<in T> : IFormatter
{
    /// <summary>
    /// Appends the description of an object into given <see cref="UnsafeStringBuilder"/> (strongly-typed variant).
    /// </summary>
    /// <param name="stringBuilder">The target <see cref="UnsafeStringBuilder"/>.</param>
    /// <param name="value">The value to be formatted.</param>
    void Format( UnsafeStringBuilder stringBuilder, T? value );
}