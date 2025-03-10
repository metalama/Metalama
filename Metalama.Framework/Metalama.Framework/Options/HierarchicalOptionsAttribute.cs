// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Options;

#pragma warning disable SA1623

/// <summary>
/// Custom attribute that, when applied to a class implementing the <see cref="IHierarchicalOptions"/> interface,
/// specifies how the options are inherited.
/// </summary>
/// <seealso href="@exposing-options"/>
[PublicAPI]
[RunTimeOrCompileTime]
[AttributeUsage( AttributeTargets.Class )]
public sealed class HierarchicalOptionsAttribute : Attribute
{
    internal static HierarchicalOptionsAttribute Default { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating that the options are inherited from the base type to derived types.
    /// </summary>
    public bool InheritedByDerivedTypes { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating that the options are inherited from the base virtual member to the overridden members. 
    /// </summary>
    public bool InheritedByOverridingMembers { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating that the options are inherited from the enclosing type to the nested types.
    /// </summary>
    public bool InheritedByNestedTypes { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating that the options are inherited from the declaring type to its members.
    /// </summary>
    public bool InheritedByMembers { get; init; } = true;
}