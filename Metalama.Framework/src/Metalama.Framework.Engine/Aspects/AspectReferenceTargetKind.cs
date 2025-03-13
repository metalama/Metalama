// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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