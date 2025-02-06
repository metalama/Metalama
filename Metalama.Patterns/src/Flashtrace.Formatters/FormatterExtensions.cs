// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace.Formatters;

/// <summary>
/// Extension methods for the <see cref="IFormatter"/> interface.
/// </summary>
[PublicAPI]
public static class FormatterExtensions
{
    /// <summary>
    /// Returns a copy of the current formatter, but for different options.
    /// </summary>
    /// <param name="formatter">The original formatter.</param>
    /// <param name="options">The new options.</param>
    /// <returns>A copy of the current formatter with the new <paramref name="options"/>.</returns>
    public static IFormatter<T> WithOptions<T>( this IFormatter<T> formatter, FormattingOptions options )
    {
        if ( formatter is IOptionAwareFormatter optionAwareFormatter )
        {
            return (IFormatter<T>) optionAwareFormatter.WithOptions( options );
        }
        else
        {
            return formatter;
        }
    }

    /// <summary>
    /// Returns a copy of the current formatter, but for different options.
    /// </summary>
    /// <param name="formatter">The original formatter.</param>
    /// <param name="options">The new options.</param>
    /// <returns>A copy of the current formatter with the new <paramref name="options"/>.</returns>
    public static IFormatter WithOptions( this IFormatter formatter, FormattingOptions? options )
    {
        if ( formatter is IOptionAwareFormatter optionAwareFormatter )
        {
            return optionAwareFormatter.WithOptions( options ?? FormattingOptions.Default );
        }
        else
        {
            return formatter;
        }
    }
}