// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// This feature is not implemented in Metalama and there is no workaround.
    /// </summary>
    public interface IOnStateMachineBoundaryAspect : IOnMethodBoundaryAspect
    {
        /// <summary>
        /// This feature is not implemented in Metalama and there is no workaround.
        /// </summary>
        [Obsolete( "", true )]
        void OnResume( MethodExecutionArgs args );

        /// <summary>
        /// This feature is not implemented in Metalama and there is no workaround.
        /// </summary>
        [Obsolete( "", true )]
        void OnYield( MethodExecutionArgs args );
    }
}