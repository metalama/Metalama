// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.Aspects
{
    /// <summary>
    /// Provides flags on aspect reference, guiding the aspect linker.
    /// </summary>
    [Flags]
    internal enum AspectReferenceFlags
    {
        /// <summary>
        /// No flags are active on the aspect reference.
        /// </summary>
        None = 0,

        /// <summary>
        /// The reference is inlineable.
        /// </summary>
        Inlineable = 1,

        /// <summary>
        /// The reference has a custom receiver (i.e. it cannot be substituted with <c>this</c> or <c>base</c>).
        /// </summary>
        CustomReceiver = 2
    }
}