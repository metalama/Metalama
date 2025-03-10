// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Describes the kind of constructor initializer.
    /// </summary>
    [CompileTime]
    public enum ConstructorInitializerKind
    {
        /// <summary>
        /// The constructor has no explicit initializer.
        /// </summary>
        None,

        /// <summary>
        /// The initializer refers to the base constructor.
        /// </summary>
        Base,

        /// <summary>
        /// The initializer reference another constructor of the same type.
        /// </summary>
        This
    }
}