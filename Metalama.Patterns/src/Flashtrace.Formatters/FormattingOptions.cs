// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Flashtrace.Formatters;

/// <summary>
/// Options that influence the formatting of an object by an <see cref="IOptionAwareFormatter"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class can be extended by implementations of custom back-end.
/// </para>
/// <para>
/// It is essential for performance that the implementation respects a semi-singleton pattern, i.e. to keep a single instance of distinct value.
/// </para>
/// </remarks>
[PublicAPI]
public record FormattingOptions( bool RequiresUnquotedStrings )
{
    /// <summary>
    /// Gets the default <see cref="FormattingOptions"/>.
    /// </summary>
    public static FormattingOptions Default { get; } = new( false );

    /// <summary>
    /// Gets the <see cref="FormattingOptions"/> indicating that string should not be quoted.
    /// </summary>
    public static FormattingOptions Unquoted { get; } = new( true );

    /// <summary>
    /// Initializes a new instance of the <see cref="FormattingOptions"/> class by copying all values from another <see cref="FormattingOptions"/>.
    /// </summary>
    /// <param name="prototype">The <see cref="FormattingOptions"/> instance whose values have to be copied.</param>
    public FormattingOptions( FormattingOptions prototype )
    {
        if ( prototype == null )
        {
            throw new ArgumentNullException( nameof(prototype) );
        }

        this.RequiresUnquotedStrings = prototype.RequiresUnquotedStrings;
    }
}