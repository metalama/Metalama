// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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