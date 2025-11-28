// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Options;

#pragma warning disable SA1623

/// <summary>
/// Custom attribute that, when applied to a class implementing the <see cref="IHierarchicalOptions"/> interface,
/// specifies how the options are inherited across different declaration levels.
/// </summary>
/// <remarks>
/// <para>
/// By default, hierarchical options are inherited along all axes: from base types to derived types, from base members to overriding members,
/// from enclosing types to nested types, and from declaring types to their members. This attribute allows aspect authors to disable
/// inheritance along specific axes when the default behavior is not appropriate for their options.
/// </para>
/// <para>
/// For example, an aspect might want options to be inherited from a type to its members, but not from a base type to derived types.
/// This can be achieved by setting <see cref="InheritedByDerivedTypes"/> to <c>false</c> while leaving other properties as <c>true</c>.
/// </para>
/// </remarks>
/// <seealso cref="ApplyChangesAxis"/>
/// <seealso href="@exposing-options"/>
/// <seealso href="@configuration-custom-merge"/>
[PublicAPI]
[RunTimeOrCompileTime]
[AttributeUsage( AttributeTargets.Class )]
public sealed class HierarchicalOptionsAttribute : Attribute
{
    internal static HierarchicalOptionsAttribute Default { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the options are inherited from the base type to derived types.
    /// The default value is <c>true</c>.
    /// </summary>
    /// <value>
    /// <c>true</c> if options set on a base type apply to derived types (unless overridden); otherwise, <c>false</c>.
    /// </value>
    public bool InheritedByDerivedTypes { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the options are inherited from the base virtual member to the overriding members.
    /// The default value is <c>true</c>.
    /// </summary>
    /// <value>
    /// <c>true</c> if options set on a base <c>virtual</c> or <c>abstract</c> member apply to <c>override</c> members in derived types
    /// (unless overridden); otherwise, <c>false</c>.
    /// </value>
    public bool InheritedByOverridingMembers { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the options are inherited from the enclosing type to the nested types.
    /// The default value is <c>true</c>.
    /// </summary>
    /// <value>
    /// <c>true</c> if options set on a type apply to types nested within it (unless overridden); otherwise, <c>false</c>.
    /// </value>
    public bool InheritedByNestedTypes { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the options are inherited from the declaring type to its members.
    /// The default value is <c>true</c>.
    /// </summary>
    /// <value>
    /// <c>true</c> if options set on a type apply to its members (methods, properties, fields, etc.) unless overridden; otherwise, <c>false</c>.
    /// </value>
    public bool InheritedByMembers { get; init; } = true;
}