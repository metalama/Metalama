// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.CodeFixes;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Services;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Extensibility;

[PublicAPI]
public sealed class DesignTimeExtensionManager : IGlobalService
{
    private readonly DesignTimeProcessKind _processKind;

    private readonly ConcurrentDictionary<int, int> _processedOptions = new();
    private readonly CompileTimeDomain _domain;
    private readonly ConcurrentDictionary<Type, IDesignTimeExtension> _extensions = new();
    private readonly IRpcServiceProviderServerEndpointProvider? _rpcServiceProviderServerEndpointProvider;
    private readonly IExtensionLoader _extensionLoader;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<IDesignTimeExtension>> _extensionsByName = new();

    private GlobalServiceProvider ServiceProvider { get; }

    public DesignTimeExtensionManager( GlobalServiceProvider serviceProvider, DesignTimeProcessKind processKind = DesignTimeProcessKind.Default )
    {
        this._processKind = processKind;
        this.ServiceProvider = serviceProvider;
        this._domain = serviceProvider.GetRequiredService<CompileTimeDomain>();
        this._rpcServiceProviderServerEndpointProvider = serviceProvider.GetService<IRpcServiceProviderServerEndpointProvider>();
        this._extensionLoader = serviceProvider.GetRequiredService<IExtensionLoader>();
    }

    internal void OnProjectDiscovered( IProjectOptions options )
    {
        if ( !this._processedOptions.TryAdd( options.Id, options.Id ) )
        {
            // The project was already processed.
            return;
        }

        lock ( this._extensions )
        {
            var extensionTypes = this._extensionLoader.GetExtensionTypes( options, this._domain, ExtensionKind.DesignTime );

            foreach ( var extensionType in extensionTypes )
            {
                if ( this._extensions.ContainsKey( extensionType ) )
                {
                    continue;
                }

                var extension = (IDesignTimeExtension) Activator.CreateInstance( extensionType, true ).AssertNotNull();
                this._extensions.TryAdd( extensionType, extension );

                this.OnExtensionDiscovered( extension );
            }
        }
    }

    public IEnumerable<IDesignTimeExtension> Extensions => this._extensions.Values;

    public ImmutableArray<ICodeFixProviderExtension> CodeFixProviderExtensions { get; private set; } = ImmutableArray<ICodeFixProviderExtension>.Empty;

    public ImmutableArray<ICodeRefactoringProviderExtension> CodeRefactoringProviderExtensions { get; private set; } =
        ImmutableArray<ICodeRefactoringProviderExtension>.Empty;

    private TaskCompletionSource<IDesignTimeExtension> GetExtensionAwaiter( string extensionName )
        => this._extensionsByName.GetOrAdd( extensionName, n => new TaskCompletionSource<IDesignTimeExtension>() );

    public Task<IDesignTimeExtension> GetExtensionAsync( string extensionName, CancellationToken cancellationToken )
        => this.GetExtensionAwaiter( extensionName ).Task.WithCancellation( cancellationToken );

    internal void OnExtensionDiscovered( IDesignTimeExtension extension )
    {
        var context = new DesignTimeInitializationContext( this.ServiceProvider, this._processKind );

        if ( extension.Initialize( context ) )
        {
            this.CodeFixProviderExtensions = this.CodeFixProviderExtensions.AddRange( context.CodeFixProviderExtensions );
            this.CodeRefactoringProviderExtensions = this.CodeRefactoringProviderExtensions.AddRange( context.CodeRefactoringProviderExtensions );

            this._rpcServiceProviderServerEndpointProvider?.Endpoint.AddServices( context.RpcServices );

            this.GetExtensionAwaiter( extension.Name ).SetResult( extension );
        }
    }
}