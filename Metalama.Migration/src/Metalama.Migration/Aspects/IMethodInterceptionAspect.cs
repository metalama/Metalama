// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In Metalama, use <see cref="OverrideMethodAspect"/>.
    /// </summary>
    public interface IMethodInterceptionAspect : IMethodLevelAspect
    {
        /// <summary>
        /// In Metalama, implement <see cref="OverrideMethodAspect.OverrideMethod"/>.
        /// </summary>
        void OnInvoke( MethodInterceptionArgs args );
    }
}