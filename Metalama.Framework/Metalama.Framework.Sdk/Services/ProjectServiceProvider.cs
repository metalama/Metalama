// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Services;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Services;

/// <summary>
/// Gives access to project-scoped services. A wrapper around <see cref="ServiceProvider{T}"/> for <see cref="IProjectService"/>.
/// </summary>
[PublicAPI]
public readonly struct ProjectServiceProvider
{
    public ServiceProvider<IProjectService> Underlying { get; }

    private readonly ServiceProvider<IGlobalService>? _global;

    public GlobalServiceProvider Global => this._global ?? throw new InvalidOperationException();

    private ProjectServiceProvider( ServiceProvider<IProjectService> serviceProvider )
    {
        this.Underlying = serviceProvider;

        // We cache the global service provider because it is used often.
        this._global = this.Underlying.FindNext<IGlobalService>();
    }

    public T GetRequiredService<T>()
        where T : class, IProjectService
        => this.Underlying.GetService<T>() ?? throw new InvalidOperationException( $"Cannot get the service {typeof(T)}." );

    public T? GetService<T>()
        where T : class, IProjectService
        => this.Underlying.GetService<T>();

    public static implicit operator ProjectServiceProvider( ServiceProvider<IProjectService> serviceProvider ) => new( serviceProvider );

    public static implicit operator ServiceProvider<IProjectService>( in ProjectServiceProvider serviceProvider ) => serviceProvider.Underlying;

    public static implicit operator GlobalServiceProvider( in ProjectServiceProvider serviceProvider ) => serviceProvider.Global;

    public ProjectServiceProvider WithService( IProjectService service, bool allowOverride = false ) => this.Underlying.WithService( service, allowOverride );

    public ServiceProvider<IProjectService> WithServices( params IProjectService[] services ) => this.Underlying.WithServices( services );

    public ServiceProvider<IProjectService> WithServices( IEnumerable<IProjectService> services ) => this.Underlying.WithServices( services );

    public override string ToString() => this.Underlying.ToString();
}