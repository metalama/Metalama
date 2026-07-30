// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable CA1822 // Mark members as static

namespace Issue1743.App
{
    /// <summary>
    /// The type the aspect is applied to, so that the aspect weaver is actually required by this compilation.
    /// </summary>
    /// <remarks>
    /// See the README of the parent directory. Two referenced assemblies provide the weaver named by
    /// <c>[Virtualize]</c>, so the compilation of this project is expected to fail with <c>LAMA0077</c>.
    /// </remarks>
    [Virtualize]
    public class Target
    {
        /// <summary>
        /// A method the weaver would make virtual.
        /// </summary>
        public void Bar() { }
    }
}
