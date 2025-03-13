// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Threading.Tasks;

namespace PostSharp.Aspects
{
    /// <summary>
    /// No equivalent in Metalama. To override an async method, implement the <see cref="OverrideMethodAspect"/>.<see cref="OverrideMethodAspect.OverrideAsyncMethod"/>
    /// method and call <see cref="meta"/>.<see cref="meta.ProceedAsync"/>.
    /// </summary>
    public struct MethodBindingInvokeAwaitable
    {
        public MethodBindingInvokeAwaiter GetAwaiter()
        {
            throw new NotImplementedException();
        }

        public Task GetTask()
        {
            throw new NotImplementedException();
        }
    }
}