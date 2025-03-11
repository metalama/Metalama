// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Flashtrace.Formatters;

/// <summary>
/// An interface that implementations of <see cref="IFormattable"/> can optionally implement to support options.
/// </summary>
[PublicAPI]
public interface IOptionAwareFormatter : IFormatter
{
    /// <summary>
    /// Returns a copy of the current formatter, but for different options.
    /// </summary>
    /// <param name="options">The new options.</param>
    /// <returns>A copy of the current formatter with the new <paramref name="options"/>.</returns>
    /// <remarks>
    /// It is essential for performance that the implementation respects a semi-singleton pattern, i.e. to keep a single instance of the formatter
    /// for each single distinct value of <see cref="FormattingOptions"/>.
    /// </remarks> 
    IOptionAwareFormatter WithOptions( FormattingOptions options );
}