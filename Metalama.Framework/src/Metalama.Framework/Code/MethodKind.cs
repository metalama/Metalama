// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Kinds of <see cref="IMethodBase"/>.
    /// </summary>
    [CompileTime]
    public enum MethodKind
    {
        /// <summary>
        /// Default.
        /// </summary>
        Default,

        /// <summary>
        /// Finalizer (destructor).
        /// </summary>
        Finalizer,

        /// <summary>
        /// Property getter.
        /// </summary>
        PropertyGet,

        /// <summary>
        /// Property setter.
        /// </summary>
        PropertySet,

        /// <summary>
        /// Event adder.
        /// </summary>
        EventAdd,

        /// <summary>
        /// Event remover.
        /// </summary>
        EventRemove,

        /// <summary>
        /// Event raiser.
        /// </summary>
        EventRaise,

        // DelegateInvoke
        // FunctionPointerSignature

        /// <summary>
        /// Explicit interface implementation.
        /// </summary>
        ExplicitInterfaceImplementation,

        /// <summary>
        /// Operator.
        /// </summary>
        Operator,

        /// <summary>
        /// Local function.
        /// </summary>
        LocalFunction,

        /// <summary>
        /// Lambda.
        /// </summary>
        Lambda,

        /// <summary>
        /// Delegate invocation.
        /// </summary>
        DelegateInvoke
    }
}