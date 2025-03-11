// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Framework.Services;
using System;
using System.Collections.Concurrent;

namespace Metalama.Framework.Engine.Services;

/// <summary>
/// A set of mocks or services injected into the production service providers.
/// </summary>
/// <remarks>
/// This object is a service itself. The test runner registers it as a global service because some pipelines
/// recreate the service providers from the global provider.
/// </remarks>
[PublicAPI]
public sealed class AdditionalServiceCollection : IAdditionalServiceCollection
{
    private readonly ConcurrentStack<IDisposable> _disposables = new();

    public AdditionalServiceCollection() { }

    public AdditionalServiceCollection( AdditionalServiceCollection? prototype )
    {
        if ( prototype != null )
        {
            this.GlobalServices = new ServiceProviderBuilder<IGlobalService>( prototype.GlobalServices );
            this.ProjectServices = new ServiceProviderBuilder<IProjectService>( prototype.ProjectServices );
            this.BackstageServices = new ServiceProviderBuilder<IBackstageService>( prototype.BackstageServices );
        }
    }

    public AdditionalServiceCollection( params IService[] additionalServices ) : this()
    {
        foreach ( var service in additionalServices )
        {
            switch ( service )
            {
                case IProjectService projectService:
                    this.ProjectServices.Add( projectService );

                    break;

                case IGlobalService globalService:
                    this.GlobalServices.Add( globalService );

                    break;

                // ReSharper disable once SuspiciousTypeConversion.Global
                case IBackstageService backstageService:
                    this.BackstageServices.Add( backstageService );

                    break;

                default:
                    throw new ArgumentException( $"The object '{service}' is not a valid service." );
            }

            if ( service is IDisposable disposable )
            {
                this._disposables.Push( disposable );
            }
        }
    }

    public ServiceProviderBuilder<IGlobalService> GlobalServices { get; } = new();

    public ServiceProviderBuilder<IProjectService> ProjectServices { get; } = new();

    public ServiceProviderBuilder<IBackstageService> BackstageServices { get; } = new();

    public void Dispose()
    {
        while ( this._disposables.TryPop( out var disposable ) )
        {
            disposable.Dispose();
        }
    }

    public void AddProjectService<T>( T service, bool allowOverride = false )
        where T : IProjectService
        => this.ProjectServices.Add( service, allowOverride );

    public void AddGlobalService<T>( T service, bool allowOverride = false )
        where T : IGlobalService
        => this.GlobalServices.Add( service, allowOverride );

    public void AddProjectService<T>( Func<ProjectServiceProvider, T> service, bool allowOverride = false )
        where T : class, IProjectService
        => this.ProjectServices.Add( provider => service( provider ), allowOverride );

    public void AddGlobalService<T>( Func<GlobalServiceProvider, T> service, bool allowOverride = false )
        where T : class, IGlobalService
        => this.GlobalServices.Add( provider => service( provider ), allowOverride );

    public void AddUntypedProjectService( Type serviceType, object implementation, bool allowOverride = false )
        => this.ProjectServices.AddUntyped( serviceType, implementation, allowOverride );

    public void AddUntypedGlobalService( Type serviceType, object implementation, bool allowOverride = false )
        => this.GlobalServices.AddUntyped( serviceType, implementation, allowOverride );
}