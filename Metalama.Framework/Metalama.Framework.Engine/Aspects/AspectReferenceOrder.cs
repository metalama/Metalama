// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.Aspects
{
    internal enum AspectReferenceOrder
    {
        /// <summary>
        /// Represents the state of the semantic before the current aspect layer. If the semantic was introduced as <c>override</c> or <c>new</c>
        /// and the reference originates in the first override, it means a call to "base".
        /// </summary>
        Base,

        /// <summary>
        /// Represents the state of a semantic before the current transformation has been applied. Used by <c>meta.Proceed()</c>.
        /// </summary>
        Previous,

        /// <summary>
        /// Represents the semantic in the state it is after the current aspect layer has been applied.
        /// </summary>
        Current,

        /// <summary>
        /// Gets the final state of the semantic with all transformations. If the semantic is virtual, this results in a virtual call.
        /// Otherwise, this results in a call to the semantic with all transformations of the current class (but not of derived classes).
        /// </summary>
        Final
    }
}