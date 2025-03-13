// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.Observers
{
    /// <summary>
    /// An interface that can be injected into the service provider to get callbacks from the aspect pipeline when the initial <see cref="ICompilation"/> is created.
    /// For testing only.
    /// </summary>
    public interface ICompilationModelObserver : IProjectService
    {
        /// <summary>
        /// Method called by the aspect pipeline when the initial <see cref="ICompilation"/> is created.
        /// </summary>
        /// <param name="compilation"></param>
        void OnInitialCompilationModelCreated( ICompilation compilation );
    }

    public interface ILinkerObserver : IProjectService
    {
        void OnIntermediateCompilationCreated( PartialCompilation compilation );
    }
}