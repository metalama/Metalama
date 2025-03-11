// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Flashtrace.Formatters;

/// <summary>
/// Attributes of an <see cref="IFormatter"/>.
/// </summary>
[PublicAPI]
[Flags]
public enum FormatterAttributes
{
    /// <summary>
    /// Default.
    /// </summary>
    None = 0,

    /// <summary>
    /// A normal (custom) formatter.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// A dynamic formatter, which resolves to another formatter according to the type of the value, not the type of the location.
    /// </summary>
    Dynamic = 2,

    /// <summary>
    /// A converter.
    /// </summary>
    Converter = 4,

    /// <summary>
    /// A default formatter, using <see cref="object.ToString"/>.
    /// </summary>
    Default = 8
}