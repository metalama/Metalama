// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Scope of introduction advice.
    /// </summary>
    [CompileTime]
    public enum IntroductionScope
    {
        /// <summary>
        /// If the advice template is static, the behavior is the same as <see cref="Static"/>, otherwise behavior is the same as <see cref="Target"/>.
        /// </summary>
        Default,

        /// <summary>
        /// Introduced member will be always of instance scope.
        /// </summary>
        Instance,

        /// <summary>
        /// Introduced member will be always of static scope.
        /// </summary>
        Static,

        /// <summary>
        /// Introduced member will be always of the same scope as the target declaration.
        /// </summary>
        Target
    }
}