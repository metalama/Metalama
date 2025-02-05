// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Framework.Engine.Formatting;

[PublicAPI]
public enum CodeFormattingOptions
{
    /// <summary>
    /// A correct C# file must be generated, but it must not be nicely formatted.
    /// </summary>
    Default,

    /// <summary>
    /// No text output is required, only a syntax tree.
    /// </summary>
    None,

    /// <summary>
    /// The C# code must be nicely formatted.
    /// </summary>
    Formatted
}