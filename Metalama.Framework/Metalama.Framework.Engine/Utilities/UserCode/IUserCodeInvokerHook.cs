// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Services;
using System;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Utilities.UserCode
{
    /// <summary>
    /// Defines a method <see cref="Invoke{TResult,TPayload}"/> that is invoked when user-written code must be invoked.
    /// </summary>
    [PublicAPI] // Implemented by Metalama.Try.
    public interface IUserCodeInvokerHook : IGlobalService
    {
        /// <summary>
        /// The implementation of this method must call the delegate given delegate. It can wrap the call with any logic.
        /// </summary>
        TResult Invoke<TResult, TPayload>( UserCodeFunc<TResult, TPayload> func, ref TPayload payload );

        /// <summary>
        /// The implementation of this method must call the delegate given delegate. It can wrap the call with any logic.
        /// </summary>
        Task<TResult> InvokeAsync<TResult>( Func<Task<TResult>> func );
    }
}