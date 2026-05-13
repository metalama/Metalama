// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.CodeFixes;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
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
    private readonly ICompileTimeDomainFactory _domainFactory;
    private readonly ConcurrentDictionary<Type, IDesignTimeExtension> _extensions = new();
    private readonly IRpcServiceProviderServerEndpointProvider? _rpcServiceProviderServerEndpointProvider;
    private readonly IExtensionLoader _extensionLoader;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<IDesignTimeExtension>> _extensionsByName = new();
    private readonly ILogger _logger;

    private GlobalServiceProvider ServiceProvider { get; }

    public DesignTimeExtensionManager( GlobalServiceProvider serviceProvider, DesignTimeProcessKind processKind = DesignTimeProcessKind.Default )
    {
        this._processKind = processKind;
        this.ServiceProvider = serviceProvider;
        this._domainFactory = serviceProvider.GetRequiredService<ICompileTimeDomainFactory>();
        this._rpcServiceProviderServerEndpointProvider = serviceProvider.GetService<IRpcServiceProviderServerEndpointProvider>();
        this._extensionLoader = serviceProvider.GetRequiredService<IExtensionLoader>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(DesignTimeExtensionManager) );
    }

    internal void OnProjectDiscovered( IProjectOptions options )
    {
        if ( !options.IsFrameworkEnabled )
        {
            // Not a Metalama project.
            return;
        }

        if ( !this._processedOptions.TryAdd( options.Id, options.Id ) )
        {
            // The project was already processed.
            return;
        }

        this._logger.Trace?.Log( $"OnProjectDiscovered('{options.ProjectPath}', '{options.TargetFramework}', '{options.Configuration}')" );

        lock ( this._extensions )
        {
            // Get a domain compatible with this project's design-time extension assemblies.
            var extensionAssemblyPaths = new List<string>( this._extensionLoader.GetExtensionAssemblyPaths( options.DesignTimeExtensionAssemblies ) );

            var domain = this._domainFactory.GetOrCreateDomain( extensionAssemblyPaths );

            var extensions = this._extensionLoader.GetExtensionTypes( options, domain, ExtensionKinds.DesignTime, NullDiagnosticAdder.Instance );

            foreach ( var extensionType in extensions )
            {
                // Dedup by Type.FullName rather than Type reference identity: the same logical extension can
                // resolve to different Type objects when DefaultCompileTimeDomainFactory hands us different
                // CompileTimeDomains for different projects, each with its own MetalamaAssemblyLoadContext.
                // Without this, the per-project extension list accumulates duplicates and aspect-emitted code
                // actions appear multiple times in the IDE menu (#1628).
                var existing = this._extensions.Keys.FirstOrDefault( t => t.FullName == extensionType.ExtensionType.FullName );

                if ( existing != null )
                {
                    if ( !ReferenceEquals( existing.Assembly, extensionType.ExtensionType.Assembly ) )
                    {
                        this._logger.Trace?.Log(
                            $"Extension '{extensionType.ExtensionType.FullName}' already loaded from a different Assembly "
                            + $"('{existing.Assembly.FullName}' vs '{extensionType.ExtensionType.Assembly.FullName}'); skipping." );
                    }
                    else
                    {
                        this._logger.Trace?.Log( $"The extension '{extensionType}' was already loaded." );
                    }

                    continue;
                }

                this._logger.Trace?.Log( $"Loading extension '{extensionType}'." );
                var extension = (IDesignTimeExtension) Activator.CreateInstance( extensionType.ExtensionType, true ).AssertNotNull();
                this._extensions.TryAdd( extensionType.ExtensionType, extension );

                this.OnExtensionDiscovered( extension );
            }
        }
    }

    public IEnumerable<IDesignTimeExtension> Extensions => this._extensions.Values;

    public ImmutableArray<ICodeFixProviderExtension> CodeFixProviderExtensions { get; private set; } = ImmutableArray<ICodeFixProviderExtension>.Empty;

    public ImmutableArray<ICodeRefactoringProviderExtension> CodeRefactoringProviderExtensions { get; private set; } =
        ImmutableArray<ICodeRefactoringProviderExtension>.Empty;

    private TaskCompletionSource<IDesignTimeExtension> GetExtensionAwaiter( string extensionName )
        => this._extensionsByName.GetOrAdd( extensionName, _ => new TaskCompletionSource<IDesignTimeExtension>() );

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

            this._logger.Trace?.Log( $"The extension '{extension.GetType()}' is now initialized." );
            this.GetExtensionAwaiter( extension.Name ).SetResult( extension );
        }
        else
        {
            this._logger.Warning?.Log( $"The extension '{extension.GetType()}' was not initialized." );
        }
    }
}