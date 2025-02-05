// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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