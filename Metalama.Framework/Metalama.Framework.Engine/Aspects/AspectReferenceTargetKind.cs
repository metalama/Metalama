// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.Aspects
{
    /// <summary>
    /// Provides kinds aspect reference targets.
    /// </summary>
    internal enum AspectReferenceTargetKind : byte
    {
        /// <summary>
        /// Target the annotated declaration.
        /// </summary>
        Self,

        /// <summary>
        /// Target the get accessor of the referenced property.
        /// </summary>
        PropertyGetAccessor,

        /// <summary>
        /// Target the set accessor of the referenced property.
        /// </summary>
        PropertySetAccessor,

        /// <summary>
        /// Target the add accessor of the referenced event.
        /// </summary>
        EventAddAccessor,

        /// <summary>
        /// Target the remove accessor of the referenced event.
        /// </summary>
        EventRemoveAccessor,

        /// <summary>
        /// Target the raise accessor of the referenced event.
        /// </summary>
        EventRaiseAccessor
    }
}