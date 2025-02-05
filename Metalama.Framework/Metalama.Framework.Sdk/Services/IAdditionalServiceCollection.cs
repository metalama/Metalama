// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Services;
using System;

namespace Metalama.Framework.Engine.Services;

/// <summary>
/// A set of mocks or services injected into the production service providers.
/// </summary>
[PublicAPI]
public interface IAdditionalServiceCollection : IGlobalService, IDisposable
{
    void AddProjectService<T>( T service, bool allowOverride = false )
        where T : IProjectService;

    void AddGlobalService<T>( T service, bool allowOverride = false )
        where T : IGlobalService;

    void AddProjectService<T>( Func<ProjectServiceProvider, T> service, bool allowOverride = false )
        where T : class, IProjectService;

    void AddGlobalService<T>( Func<GlobalServiceProvider, T> service, bool allowOverride = false )
        where T : class, IGlobalService;

    void AddUntypedProjectService( Type serviceType, object implementation, bool allowOverride = false );

    void AddUntypedGlobalService( Type serviceType, object implementation, bool allowOverride = false );
}