// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Flashtrace.Formatters;

/// <summary>
/// Describes a formatting role.
/// </summary>
[PublicAPI]
public class FormattingRole
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormattingRole"/> class.
    /// </summary>
    public FormattingRole( string? name = null )
    {
        this.Name = name ?? this.GetType().Name;
    }

    /// <summary>
    /// Gets the name of the <see cref="FormattingRole"/>.
    /// </summary>
    public string Name { get; }

    public override string ToString() => this.Name;
}