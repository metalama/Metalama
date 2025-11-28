// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Enumerates the different abilities of a field or property to be written (set).
    /// Values are ordered from most restrictive (<see cref="None"/>) to least restrictive (<see cref="All"/>).
    /// </summary>
    /// <seealso cref="IFieldOrPropertyOrIndexer"/>
    /// <seealso cref="IField"/>
    /// <seealso cref="IProperty"/>
    [CompileTime]
    public enum Writeability
    {
        // IMPORTANT: Do not change values, comparison depends on these.

        /// <summary>
        /// The field or property cannot be set at any time (e.g., a <c>const</c> field or a read-only property with only a <c>get</c> accessor).
        /// </summary>
        None = 0,

        /// <summary>
        /// The field or property can only be set from constructors of the declaring type (e.g., a <c>readonly</c> field or a property with only an <c>init</c> accessor).
        /// </summary>
        ConstructorOnly = 1,

        /// <summary>
        /// The property can be set from constructors or from object initializers (e.g., a property with an <c>init</c> accessor).
        /// </summary>
        InitOnly = 2,

        /// <summary>
        /// The field or property can be set at any time (e.g., a non-<c>readonly</c> field or a property with a <c>set</c> accessor).
        /// </summary>
        All = 3
    }
}