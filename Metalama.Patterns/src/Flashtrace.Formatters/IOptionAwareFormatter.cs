// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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