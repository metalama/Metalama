// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.CodeFixes;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
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
    private readonly RpcServiceProviderServerEndpoint? _rpcServiceProvider;
    private readonly IExtensionLoader _extensionLoader;

    private GlobalServiceProvider ServiceProvider { get; }

    public DesignTimeExtensionManager( GlobalServiceProvider serviceProvider, DesignTimeProcessKind processKind = DesignTimeProcessKind.Default )
    {
        this._processKind = processKind;
        this.ServiceProvider = serviceProvider;
        this._domain = serviceProvider.GetRequiredService<CompileTimeDomain>();
        this._rpcServiceProvider = serviceProvider.GetService<RpcServiceProviderServerEndpoint>();
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

    private void OnExtensionDiscovered( IDesignTimeExtension extension )
    {
        var context = new DesignTimeInitializationContext( this.ServiceProvider, this._processKind );

        if ( extension.Initialize( context ) )
        {
            this.CodeFixProviderExtensions = this.CodeFixProviderExtensions.AddRange( context.CodeFixProviderExtensions );
            this.CodeRefactoringProviderExtensions = this.CodeRefactoringProviderExtensions.AddRange( context.CodeRefactoringProviderExtensions );

            this._rpcServiceProvider?.AddServices( context.RpcServices );
        }
    }
}