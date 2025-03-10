// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Utilities;
using Metalama.Compiler;
using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.DesignTime.VersionNeutral;
using Metalama.Framework.DesignTime.VisualStudio.Rpc;
using Metalama.Framework.DesignTime.VisualStudio.Services;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Services;

namespace Metalama.Framework.DesignTime.Services;

/// <summary>
/// The three different kinds of process that affect the architecture of the design-time components.
/// </summary>
public enum DesignTimeProcessKind
{
    /// <summary>
    /// By default, all design-time components run in the same process.
    /// </summary>
    Default,

    /// <summary>
    /// The user process of Visual Studio hosts the entry points of fixes/refactorings (plus the secondary entry point for code generators)
    /// and VSX services and talk to <see cref="VsAnalysisProcess"/> via RPC.
    /// </summary>
    VsUserProcess,

    /// <summary>
    /// The analysis process of Visual Studio hosts the entry points of diagnostic analyzers, diagnostic suppressors, and source generators.
    /// They serve the user-process services via RPC.
    /// </summary>
    VsAnalysisProcess
}

/// <summary>
/// A <see cref="GlobalServiceProvider"/> factory for design-time processes.
/// </summary>
internal abstract class DesignTimeServiceProviderFactory
{
    private DesignTimeProcessKind DesignTimeProcessKind { get; }

    private static readonly object _initializeSync = new();
    private static volatile DesignTimeServiceProviderFactory? _sharedFactory;
    private static volatile ServiceProvider<IGlobalService>? _sharedServiceProvider;

    private readonly IDesignTimeEntryPointManager _designTimeEntryPointManager;

    protected DesignTimeServiceProviderFactory( IDesignTimeEntryPointManager? designTimeEntryPointManager, DesignTimeProcessKind processKind )
    {
        this.DesignTimeProcessKind = processKind;
        this._designTimeEntryPointManager = designTimeEntryPointManager ?? DesignTimeEntryPointManager.Instance;
    }

    internal static ServiceProvider<IGlobalService> GetSharedServiceProvider()
    {
        return ProcessUtilities.ProcessKind switch
        {
            ProcessKind.DevEnv => GetSharedServiceProvider<VsUserProcessServiceProviderFactory>(),
            ProcessKind.RoslynCodeAnalysisService => GetSharedServiceProvider<VsAnalysisProcessServiceProviderFactory>(),
            _ => GetSharedServiceProvider<DesignTimeAnalysisProcessServiceProviderFactory>()
        };
    }

    private DesignTimeExtensionManager CreateExtensionManager( GlobalServiceProvider serviceProvider ) => new( serviceProvider, this.DesignTimeProcessKind );

    protected virtual ServiceProvider<IGlobalService> AddServices( ServiceProvider<IGlobalService> serviceProvider )
        => serviceProvider
            .WithServiceConditional<IProjectOptionsFactory>( _ => new MSBuildProjectOptionsFactory() )
            .WithServiceConditional<IUserDiagnosticRegistrationService>( sp => new UserDiagnosticRegistrationService( sp ) )
            .WithServiceConditional<DesignTimeExtensionManager>( sp => this.CreateExtensionManager( sp ) );

    protected virtual CompilerServiceProvider CreateCompilerServiceProvider() => new();

    private static ServiceProvider<IGlobalService> GetSharedServiceProvider<T>()
        where T : DesignTimeServiceProviderFactory, new()
    {
        if ( MetalamaCompilerInfo.IsActive )
        {
            throw new InvalidOperationException( "This method cannot be called from the Metalama Compiler process." );
        }

        if ( _sharedServiceProvider == null )
        {
            lock ( _initializeSync )
            {
                if ( _sharedServiceProvider == null )
                {
                    var factory = new T();

                    DesignTimeServices.Initialize();

                    if ( Logger.DesignTimeEntryPointManager.Trace != null )
                    {
                        DesignTimeEntryPointManager.Instance.SetLogger( Logger.DesignTimeEntryPointManager.Trace.Log );
                    }

                    var serviceProvider = ServiceProviderFactory.GetServiceProvider();

                    _sharedServiceProvider = factory.GetServiceProvider( serviceProvider );
                    _sharedFactory = factory;
                }
            }
        }

        if ( _sharedFactory is not T )
        {
            throw new AssertionFailedException( $"The method was already called with T being '{_sharedFactory?.GetType()}', but now it's '{typeof(T)}'." );
        }

        return _sharedServiceProvider;
    }

    internal ServiceProvider<IGlobalService> GetServiceProvider( ServiceProvider<IGlobalService> serviceProvider )
    {
        var domain = serviceProvider.GetService<CompileTimeDomain>();

        if ( domain == null )
        {
            // May be non-null in tests.
            domain = new CompileTimeDomain( serviceProvider, $"DesignTime ({this.GetType().Name})" );

            serviceProvider = serviceProvider.WithService( domain );
        }

        // Add the services that may be required by the CompilerServiceProvider.
        serviceProvider = serviceProvider
            .WithServiceConditional( sp => new DesignTimeExceptionHandler( sp ) );

        serviceProvider = serviceProvider
            .WithUntypedService( typeof(IRpcExceptionHandler), new RpcExceptionHandler( serviceProvider ), true );

        serviceProvider = serviceProvider
            .WithUntypedService( typeof(IJsonSerializationBinderProvider), new JsonSerializationBinderProvider() );

        // Create a CompilerServiceProvider.
        var compilerServiceProvider = this.CreateCompilerServiceProvider();
        var entryPointConsumer = this._designTimeEntryPointManager.GetConsumer( CurrentContractVersions.All );

        serviceProvider = serviceProvider.WithUntypedService( typeof(IDesignTimeEntryPointConsumer), entryPointConsumer );

        // Add other services.
        serviceProvider = this.AddServices( serviceProvider );

        // At this point, all services have been created, so we can initialize our CompilerServiceProvider and
        // register it. Once it is registered, consumers can start using the services immediately, so it is important
        // that all initializations are done before we register the provider to the entry point.
        compilerServiceProvider.Initialize( serviceProvider );
        this._designTimeEntryPointManager.RegisterServiceProvider( compilerServiceProvider );

        return serviceProvider;
    }

    private sealed class RpcExceptionHandler : IRpcExceptionHandler
    {
        private readonly DesignTimeExceptionHandler _exceptionHandler;

        public RpcExceptionHandler( ServiceProvider<IGlobalService> serviceProvider )
        {
            this._exceptionHandler = serviceProvider.GetService<DesignTimeExceptionHandler>()
                                     ?? throw new InvalidOperationException( "DesignTimeExceptionHandler is required." );
        }

        public void OnException( Exception e, ILogger logger, bool isDisposing ) => this._exceptionHandler.ReportException( e );
    }
}