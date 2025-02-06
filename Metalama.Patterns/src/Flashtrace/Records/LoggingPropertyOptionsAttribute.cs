// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace.Records;

/// <summary>
/// Specifies the options (<see cref="LoggingPropertyOptions"/>) of a logging property that is
/// expresses as a public property of a CLR type.
/// </summary>
[PublicAPI]
[AttributeUsage( AttributeTargets.Property )]
public sealed class LoggingPropertyOptionsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether this property is ignored, i.e. it should not be mapped to a logging property.
    /// </summary>
    public bool IsIgnored { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this property must be included into the message.
    /// </summary>
    public bool IsRendered { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this property is inherited by child contexts.
    /// </summary>
    public bool IsInherited { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this property must be carried in cross-process requests.
    /// </summary>
    public bool IsBaggage { get; set; }

    /// <summary>
    /// Converts the current attribute into a <see cref="LoggingPropertyOptions"/>.
    /// </summary>
    /// <returns></returns>
    public LoggingPropertyOptions ToOptions()
        => new(
            isRendered: this.IsRendered,
            isInherited: this.IsInherited,
            isBaggage: this.IsBaggage,
            isIgnored: this.IsIgnored );
}