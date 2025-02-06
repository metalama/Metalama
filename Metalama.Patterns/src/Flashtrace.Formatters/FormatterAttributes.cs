// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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