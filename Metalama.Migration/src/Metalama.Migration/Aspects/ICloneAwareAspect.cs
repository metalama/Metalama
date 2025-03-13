// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// There is no built-in support for cloning in Metalama at the moment.
    /// </summary>
    public interface ICloneAwareAspect : IInstanceScopedAspect
    {
        /// <summary>
        /// There is no built-in support for cloning in Metalama at the moment.
        /// </summary>
        [Obsolete( "", true )]
        void OnCloned( ICloneAwareAspect source );
    }
}