// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Advising;

/// <summary>
/// Represents the result of override advice, returned by methods such as <see cref="AdviserExtensions.Override(IAdviser{Code.IMethod}, string, object?, object?)"/>
/// and <see cref="AdviserExtensions.OverrideAccessors"/>.
/// </summary>
/// <typeparam name="T">The type of declaration that was overridden (e.g., <see cref="Code.IMethod"/>, <see cref="Code.IFieldOrProperty"/>, <see cref="Code.IEvent"/>).</typeparam>
/// <seealso cref="IAdviceResult"/>
/// <seealso cref="AdviserExtensions"/>
/// <seealso cref="AdviceOutcome"/>
/// <seealso cref="OverrideMethodAspect"/>
/// <seealso cref="OverrideFieldOrPropertyAspect"/>
/// <seealso cref="OverrideEventAspect"/>
/// <seealso href="@overriding-methods"/>
/// <seealso href="@overriding-fields-or-properties"/>
/// <seealso href="@overriding-events"/>
/// <seealso href="@overriding-constructors"/>
public interface IOverrideAdviceResult<out T> : IAdviceResult
    where T : class, IDeclaration
{
    /// <summary>
    /// Gets the declaration after the override transformation has been applied.
    /// </summary>
    /// <value>
    /// For most declarations, this returns the same declaration that was targeted by the override advice.
    /// When overriding a field, this returns the property that now represents the field (the field is automatically
    /// transformed into a property with a backing field).
    /// </value>
    T Declaration { get; }
}