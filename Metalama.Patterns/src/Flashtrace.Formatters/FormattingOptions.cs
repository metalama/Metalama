// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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