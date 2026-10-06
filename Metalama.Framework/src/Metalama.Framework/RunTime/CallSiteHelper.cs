// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.RunTime
{
    /// <summary>
    /// Methods that the generated code calls when it rewrites a call site. They are not intended to be called by user code.
    /// </summary>
    /// <remarks>
    /// When a rewritten call does not pass a value of the original call, and the value can have a side effect, the generated code still evaluates
    /// the value, in the order of the original call, by passing it to one of these methods together with an adjacent value that the rewritten call
    /// passes. C# evaluates the arguments of a call in the order in which they are written, so the order of the parameters of each method is the
    /// order of evaluation.
    /// </remarks>
    [EditorBrowsable( EditorBrowsableState.Never )]
    public static class CallSiteHelper
    {
        /// <summary>
        /// Evaluates a value that the rewritten call does not pass, then returns the value that it passes. The method preserves the order of evaluation
        /// of the original call when the dropped values precede the kept value.
        /// </summary>
        /// <typeparam name="TDrop">The type of the dropped value, or a value tuple of the dropped values when there are several.</typeparam>
        /// <typeparam name="TKeep">The type of the kept value, which is the type of the parameter that receives it.</typeparam>
        /// <param name="drop">The dropped value. It is evaluated by the caller before <paramref name="keep"/> and ignored.</param>
        /// <param name="keep">The value that the rewritten call passes.</param>
        /// <returns><paramref name="keep"/>.</returns>
        // The dropped value is passed by value and not with 'in', although it can be a value tuple of several dropped values. An 'in' argument
        // must be a variable, so the compiler stores an rvalue argument in a temporary and passes the address of that temporary. Taking the
        // address of a local can prevent the JIT compiler from keeping the local in registers. A value parameter of an inlined method becomes a
        // local that is never read, so the JIT compiler can remove the copy. The order of evaluation is the same with both forms, because the
        // argument is evaluated by the caller in both cases.
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        [DebuggerHidden]
        public static TKeep DropBefore<TDrop, TKeep>( TDrop drop, TKeep keep ) => keep;

        /// <summary>
        /// Returns a value that the rewritten call passes, then evaluates a value that it does not pass. The method preserves the order of evaluation
        /// of the original call when the dropped values follow the kept value.
        /// </summary>
        /// <typeparam name="TKeep">The type of the kept value, which is the type of the parameter that receives it.</typeparam>
        /// <typeparam name="TDrop">The type of the dropped value, or a value tuple of the dropped values when there are several.</typeparam>
        /// <param name="keep">The value that the rewritten call passes.</param>
        /// <param name="drop">The dropped value. It is evaluated by the caller after <paramref name="keep"/> and ignored.</param>
        /// <returns><paramref name="keep"/>.</returns>
        // The dropped value is passed by value for the reason given in DropBefore.
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        [DebuggerHidden]
        public static TKeep DropAfter<TKeep, TDrop>( TKeep keep, TDrop drop ) => keep;
    }
}
