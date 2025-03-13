// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.SyntaxGeneration;

// This type is copied from the Roslyn source code. The member integer values must match.
// Resharper disable UnusedMember.Global
[Flags]
public enum ObjectDisplayOptions
{
    /// <summary>Format object using default options.</summary>
    None = 0,

    /// <summary>
    /// Include the numeric code point before character literals.
    /// </summary>
    IncludeCodePoints = 1,

    /// <summary>
    /// Whether or not to include type suffix for applicable integral literals.
    /// </summary>
    IncludeTypeSuffix = 2,

    /// <summary>
    /// Whether or not to display integral literals in hexadecimal.
    /// </summary>
    UseHexadecimalNumbers = 4,

    /// <summary>
    /// Whether or not to quote character and string literals.
    /// </summary>
    UseQuotes = 8,

    /// <summary>
    /// Replace non-printable (e.g. control) characters with dedicated (e.g. \t) or unicode (\u0001) escape sequences.
    /// </summary>
    EscapeNonPrintableCharacters = 16 // 0x00000010
}