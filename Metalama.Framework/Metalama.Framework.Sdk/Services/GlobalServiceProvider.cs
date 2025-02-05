// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Services;
using System;

namespace Metalama.Framework.Engine.Services;

/// <summary>
/// Gives access to globally scoped services. A wrapper around <see cref="ServiceProvider{T}"/> for <see cref="IGlobalService"/>.
/// </summary>
[PublicAPI]
public readonly struct GlobalServiceProvider
{
    public ServiceProvider<IGlobalService> Underlying { get; }

    public bool IsNull => this.Underlying == null;

    private GlobalServiceProvider( ServiceProvider<IGlobalService> serviceProvider )
    {
        this.Underlying = serviceProvider;
    }

    public T GetRequiredService<T>()
        where T : class, IGlobalService
        => this.Underlying.GetService<T>() ?? throw new InvalidOperationException( $"Cannot get the service {typeof(T).Name}." );

    public T? GetService<T>()
        where T : class, IGlobalService
        => this.Underlying.GetService<T>();

    public static implicit operator GlobalServiceProvider( ServiceProvider<IGlobalService> serviceProvider ) => new( serviceProvider );

    public static implicit operator ServiceProvider<IGlobalService>( GlobalServiceProvider serviceProvider ) => serviceProvider.Underlying;

    public GlobalServiceProvider WithService( IGlobalService service ) => this.Underlying.WithService( service );

    public GlobalServiceProvider WithServices( params IGlobalService[] services ) => this.Underlying.WithServices( services );

    public override string ToString() => this.Underlying.ToString();
}