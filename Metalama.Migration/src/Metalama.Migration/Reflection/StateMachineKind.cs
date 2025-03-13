// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace PostSharp.Reflection
{
    /// <summary>
    /// In Metalama, use <see cref="Metalama.Framework.Code.AsyncInfo"/> at compile time. There is no equivalent at run time.
    /// </summary>
    public enum StateMachineKind
    {
        None,

        Iterator,

        Async,

        AsyncIterator
    }
}