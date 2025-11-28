// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Framework.Engine.Formatting;

/// <summary>
/// Specifies the level of formatting to apply when generating C# code.
/// </summary>
[PublicAPI]
public enum CodeFormattingOptions
{
    /// <summary>
    /// A syntactically correct C# file must be generated, but it does not need to be nicely formatted.
    /// </summary>
    Default,

    /// <summary>
    /// No text output is required, only a syntax tree.
    /// </summary>
    None,

    /// <summary>
    /// The C# code must be formatted with proper indentation and spacing.
    /// </summary>
    Formatted
}