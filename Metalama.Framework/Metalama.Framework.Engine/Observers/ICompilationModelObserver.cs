// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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