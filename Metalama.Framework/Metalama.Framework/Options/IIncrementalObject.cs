// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Options;

/// <summary>
/// A base interface for all classes whose individual instances represent incremental changes that can be combined
/// with the <see cref="ApplyChanges"/> method.
/// </summary>
/// <seealso href="@exposing-options"/>
[CompileTime]
public interface IIncrementalObject
{
    /// <summary>
    /// Returns an object where the properties of the current objects are overwritten or complemented by
    /// the properties of another given object, except if these properties are not set.
    /// </summary>
    /// <param name="changes">The object being applied on the current object, which property values, if they are set, take precedence
    /// over the ones of the current object.</param>
    /// <param name="context">Information about the context of the current operation. </param>
    /// <returns>A new immutable instance of the same class.</returns>
    object ApplyChanges( object changes, in ApplyChangesContext context );
}