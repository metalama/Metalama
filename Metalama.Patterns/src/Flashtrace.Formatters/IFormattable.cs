// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Flashtrace.Formatters;

/// <summary>
/// Exposes a <see cref="Format"/> method, which allows an object to format itself into an <see cref="UnsafeStringBuilder"/>.
/// Logging and caching components rely on the <see cref="IFormattable"/> interface.
/// </summary>
[PublicAPI]
public interface IFormattable<T>
    where T : FormattingRole
{
    /// <summary>
    /// Appends a description of the current object to a given <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">The <see cref="UnsafeStringBuilder"/> to which the object description should be written.</param>
    /// <param name="formatterRepository">The <see cref="IFormatterRepository"/> which should be used to obtain formatters.</param>
    void Format( UnsafeStringBuilder stringBuilder, IFormatterRepository formatterRepository );
}