// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;

namespace Metalama.DesignTime.Hosting;

/// <summary>
/// Options of a <see cref="DesignTimeHost"/>.
/// </summary>
public sealed class DesignTimeHostOptions
{
    /// <summary>
    /// Gets or sets the order in which the projects are analyzed when <see cref="OrderedProjectNames"/> is empty.
    /// </summary>
    public TraversalOrder Traversal { get; set; } = TraversalOrder.Solution;

    /// <summary>
    /// Gets or sets the names of the projects to analyze first, in this order. The other projects are analyzed afterwards,
    /// in the order of the solution. When the array is empty, <see cref="Traversal"/> gives the order.
    /// </summary>
    public ImmutableArray<string> OrderedProjectNames { get; set; } = ImmutableArray<string>.Empty;
}
