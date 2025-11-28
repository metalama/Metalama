// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Options;

/// <summary>
/// A base interface for all classes whose individual instances represent incremental changes that can be combined
/// with the <see cref="ApplyChanges"/> method.
/// </summary>
/// <remarks>
/// <para>
/// This interface is the foundation of Metalama's options system. Classes implementing <see cref="IIncrementalObject"/> represent
/// <i>layers of changes</i> rather than complete states. When multiple layers are combined using <see cref="ApplyChanges"/>,
/// they are merged to produce a final effective configuration.
/// </para>
/// <para>
/// Implementations must be immutable. Each instance should only store the properties that are explicitly being changed,
/// with all other properties typically set to <c>null</c> or an equivalent "unset" state. When <see cref="ApplyChanges"/> is called,
/// properties from the <c>changes</c> parameter override properties from the current instance, but only if they are set
/// (i.e., non-null or otherwise marked as "set").
/// </para>
/// <para>
/// The most common implementation of this interface is <see cref="IHierarchicalOptions"/>, which is used for aspect configuration.
/// Additionally, specialized collection types like <see cref="IncrementalHashSet{T}"/> and <see cref="IncrementalKeyedCollection{TKey,TValue}"/>
/// implement this interface to support incremental modifications of collections within options.
/// </para>
/// </remarks>
/// <seealso cref="IHierarchicalOptions"/>
/// <seealso cref="IncrementalHashSet{T}"/>
/// <seealso cref="IncrementalKeyedCollection{TKey,TValue}"/>
/// <seealso href="@exposing-options"/>
/// <seealso href="@configuration-custom-merge"/>
[CompileTime]
public interface IIncrementalObject
{
    /// <summary>
    /// Returns an object where the properties of the current object are overwritten or complemented by
    /// the properties of another given object, but only for properties that are set in the <paramref name="changes"/> object.
    /// </summary>
    /// <param name="changes">The object being applied on the current object. Property values in <paramref name="changes"/> that are set
    /// take precedence over the corresponding properties of the current object.</param>
    /// <param name="context">Information about the context of the current operation, including the <see cref="ApplyChangesContext.Axis"/>
    /// indicating along which dimension the changes are being applied.</param>
    /// <returns>A new immutable instance of the same class representing the merged result.</returns>
    /// <remarks>
    /// <para>
    /// The implementation should return a new object that combines the current instance and the <paramref name="changes"/> instance.
    /// For each property, if the property is set (non-null) in <paramref name="changes"/>, use that value; otherwise, use the value
    /// from the current instance.
    /// </para>
    /// <para>
    /// The <paramref name="context"/> parameter provides information about the merging axis (see <see cref="ApplyChangesAxis"/>),
    /// which can be used to customize the merging behavior based on how the options are being inherited or combined.
    /// </para>
    /// </remarks>
    object ApplyChanges( object changes, in ApplyChangesContext context );
}