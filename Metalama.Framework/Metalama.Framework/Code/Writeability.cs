// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Enumerates the different abilities of a field or property to be written (set).
    /// </summary>
    [CompileTime]
    public enum Writeability
    {
        // IMPORTANT: Do not change values, comparison depends on these.

        /// <summary>
        /// The field or property cannot be set (e.g. it is a read-only non-automatic property or a const field ).
        /// </summary>
        None = 0,

        /// <summary>
        /// The field or property can only be set from the constructor (e.g. it is a <c>readonly</c> field or an automatic property with a sole <c>get</c> accessor).
        /// </summary>
        ConstructorOnly = 1,

        /// <summary>
        /// The property can be set from constructor or from the initializer (e.g. it is a property with an <c>init</c> accessor).
        /// </summary>
        InitOnly = 2,

        /// <summary>
        /// The field or property can be set at all times (e.g. this is a non-<c>readonly</c> field or a property with a <c>set</c> accessor).
        /// </summary>
        All = 3
    }
}