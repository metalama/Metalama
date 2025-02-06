// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace;

/// <summary>
/// Describes a formatting role.
/// </summary>
[PublicAPI]
public class FlashtraceRole
{
    /// <summary>
    /// Gets a value indicating whether the role is used by Metalama itself.
    /// </summary>
    public bool IsSystem { get; }

    private FlashtraceRole( string? name, bool isSystem )
    {
        this.IsSystem = isSystem;
        this.Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlashtraceRole"/> class.
    /// </summary>
    public FlashtraceRole( string? name ) : this( name, false ) { }

    /// <summary>
    /// Gets the name of the <see cref="FlashtraceRole"/>. For the <see cref="Default"/> role,
    /// this property is <c>null</c>.
    /// </summary>
    public string? Name { get; }

    public override string ToString() => this.Name ?? "(default)";

    /// <summary>
    /// Gets the <see cref="FlashtraceRole"/> used by <c>Metalama.Patterns.Caching</c>.
    /// </summary>
    public static FlashtraceRole Caching { get; } = new( "Caching", true );

    /// <summary>
    /// Gets the default <see cref="FlashtraceRole"/> instance, which should be used for manual logging.
    /// </summary>
    public static FlashtraceRole Default { get; } = new( null, false );

    /// <summary>
    /// Gets the <see cref="FlashtraceRole"/> used by the logging component itself.
    /// </summary>
    public static FlashtraceRole Meta { get; } = new( "Meta", true );
}