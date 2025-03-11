// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Invokers;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In PostSharp, a binding was a run-time object that allowed the run-time code of the aspect to call the target code. In Metalama, aspects no longer
    /// have run-time code. Instead, they have templates that are expanded at compile time and generate run-time code. Templates can generate run-time code
    /// that accesses target code using dynamic code or invokers. For methods, use <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.Method"/>.<see cref="IMethodInvoker.Invoke(object[])"/>.
    /// </summary>
    public interface IMethodBinding
    {
        /// <summary>
        /// In Metalama, use <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.Method"/>.<see cref="IMethodInvoker.Invoke(object[])"/>.
        /// </summary>
        object Invoke( ref object instance, Arguments arguments );
    }
}