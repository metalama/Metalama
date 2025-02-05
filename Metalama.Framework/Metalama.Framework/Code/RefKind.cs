// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Kinds of parameters, such as <c>ref</c>, <c>in</c>, <c>out</c>...
    /// </summary>
    [CompileTime]
    public enum RefKind
    {
        /// <summary>
        /// A normal parameter passed by value.
        /// </summary>
        None,

        /// <summary>
        /// <c>ref</c>.
        /// </summary>
        Ref,

        /// <summary>
        /// <c>in</c> input parameter.
        /// </summary>
        In,

        /// <summary>
        /// <c>ref readonly</c> property, parameter, or return parameter.
        /// </summary>
        RefReadOnly,

        /// <summary>
        /// <c>out</c>.
        /// </summary>
        Out
    }
}