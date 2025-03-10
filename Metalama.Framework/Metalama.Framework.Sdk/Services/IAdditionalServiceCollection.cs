// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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