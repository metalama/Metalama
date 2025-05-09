// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Maintenance;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Project;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit.Abstractions;

namespace Metalama.Testing.UnitTesting;

/// <summary>
/// A context in which a Metalama unit test can run, configured with most required Metalama services and optionally some mocks.
/// </summary>
[PublicAPI]
public partial class TestContext : IDisposable, ITempFileManager, IApplicationInfoProvider, IDateTimeProvider
{
    private static readonly IApplicationInfo _applicationInfo = new TestApiApplicationInfo();
    private readonly ITempFileManager _backstageTempFileManager;
    private readonly bool _isRoot;
    private readonly StackTrace _stackTrace = new();
    private readonly Lazy<ImmutableArray<object>> _plugIns;
    private readonly ApplicationExitManager _applicationExitManager;

    internal TestProjectOptions TestProjectOptions { get; }

    private readonly CancellationTokenSource? _testCancellationTokenSource;
    private readonly CancellationTokenRegistration? _cancellationTokenRegistration;

    private readonly Timer? _timer;

#pragma warning disable LAMA0821 // Do not expose internal APIs.
    public IProjectOptions ProjectOptions => this.TestProjectOptions;
#pragma warning restore LAMA0821

    /// <summary>
    /// Gets the directory that was specifically created for the current test and where all specific files should be stored.
    /// </summary>
    public string BaseDirectory => this.TestProjectOptions.BaseDirectory;

    /// <summary>
    /// Gets the <see cref="ProjectServiceProvider"/> for the current context.
    /// </summary>
    public ProjectServiceProvider ServiceProvider { get; private set; }

    /// <summary>
    /// Gets a <see cref="CancellationToken"/> used to cancel the test in case of timeout. The timeout period is defined
    /// by the <see cref="TestContextOptions.Timeout"/> option.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    public ImmutableArray<object> PlugIns => this._plugIns.Value;

    // ReSharper disable once RedundantOverload.Global

    /// <summary>
    /// Initializes a new instance of the <see cref="TestContext"/> class. Tests typically
    /// do not call this constructor directly, but instead the <see cref="UnitTestClass.CreateTestContext(IAdditionalServiceCollection,string?,string?)"/>
    /// method.
    /// </summary>
    public TestContext( TestContextOptions contextOptions, CancellationToken cancellationToken = default ) : this( contextOptions, null, cancellationToken ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestContext"/> class and specify an optional <see cref="IAdditionalServiceCollection"/>. Tests typically
    /// do not call this constructor directly, but instead the <see cref="UnitTestClass.CreateTestContext(IAdditionalServiceCollection,string?,string?)"/>
    /// method.
    /// </summary>
    public TestContext(
        TestContextOptions contextOptions,
        IAdditionalServiceCollection? additionalServices,
        CancellationToken cancellationToken = default )
    {
        if ( !Debugger.IsAttached )
        {
            this._testCancellationTokenSource = new CancellationTokenSource();
            this.CancellationToken = this._testCancellationTokenSource.Token;
            this._timer = new Timer( this.OnTimeout, null, contextOptions.Timeout, contextOptions.Timeout );
            this._cancellationTokenRegistration = cancellationToken.Register( () => this._testCancellationTokenSource.Cancel() );
        }
        else
        {
            // We don't cancel tests when a debugger is attached because it's then normal that a test runs during a long time.
            this.CancellationToken = cancellationToken;
        }

        this._isRoot = true;

        this.TestProjectOptions = new TestProjectOptions( contextOptions );

        try
        {
            this._backstageTempFileManager = BackstageServiceFactory.ServiceProvider.GetRequiredBackstageService<ITempFileManager>();

            var platformInfo = BackstageServiceFactory.ServiceProvider.GetRequiredBackstageService<IPlatformInfo>();

            // We intentionally replace (override) backstage services by ours.
            var backstageServices = ServiceProvider<IBackstageService>.Empty
                .WithService( this )
                .WithService( platformInfo )
                .WithService( BackstageServiceFactory.ServiceProvider.GetRequiredBackstageService<IFileSystem>() )
                .WithService( BackstageServiceFactory.ServiceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>() );

            backstageServices = backstageServices.WithService( new InMemoryConfigurationManager( backstageServices ), true );

            var typedAdditionalServices = (AdditionalServiceCollection?) additionalServices ?? new AdditionalServiceCollection();
            typedAdditionalServices.GlobalServices.Add( sp => new TestCompileTimeDomainFactory( sp ) );
            typedAdditionalServices.GlobalServices.Add( sp => sp.WithServiceConditional<IGlobalOptions>( _ => new TestGlobalOptions() ) );
            typedAdditionalServices.GlobalServices.Add<IExtensionLoader>( _ => new TestExtensionLoader( contextOptions ), true );

            typedAdditionalServices.GlobalServices.Add( sp => sp.WithService<IProjectOptionsFactory>( _ => new TestProjectOptionsFactory(
                                                                                                          this.ProjectOptions ) ) );

            backstageServices = typedAdditionalServices.BackstageServices.Build( backstageServices );

            this.Logger = backstageServices.GetLoggerFactory().GetLogger( nameof(TestContext) );

            var serviceProvider = ServiceProviderFactory.GetServiceProvider( backstageServices, typedAdditionalServices );

            this._applicationExitManager = serviceProvider.GetRequiredService<ApplicationExitManager>();

            serviceProvider = serviceProvider
                .WithService( this.TestProjectOptions.DomainObserver );

            var randomGeneratorService = contextOptions.RunnerServiceProvider.GetService<IRandomNumberProvider>() ?? new RandomNumberProvider();
            serviceProvider = serviceProvider.WithService( randomGeneratorService );

            this.ServiceProvider = serviceProvider
                .WithProjectScopedServices( this.ProjectOptions, contextOptions.AdditionalMetadataReferences );

            this._plugIns = new Lazy<ImmutableArray<object>>( () => this.LoadPlugIns( contextOptions ) );
        }
        catch
        {
            // Avoid a misleading exception thrown by the finalizer.
            GC.SuppressFinalize( this );

            throw;
        }
    }

    public ILogger Logger { get; }

    private void OnTimeout( object? state )
    {
        this.TestOutputWriter?.WriteLine( "Timeout. Cancelling the test." );
        this._testCancellationTokenSource?.Cancel();
        this._applicationExitManager.OnApplicationExiting();
    }

    private ImmutableArray<object> LoadPlugIns( TestContextOptions options )
    {
        if ( options.TestPlugInTypes.IsDefaultOrEmpty )
        {
            return ImmutableArray<object>.Empty;
        }

        // Ensure extensions have been loaded.
        foreach ( var extensionAssembly in options.ExtensionAssemblies )
        {
            this.Domain.LoadAssembly( extensionAssembly, null, LoadAssemblyOptions.Shared );
        }

        // Load plug-ins.
        var plugIns = ImmutableArray.CreateBuilder<object>();

        foreach ( var plugInType in options.TestPlugInTypes )
        {
            var parts = plugInType.Split( ',' );
            var typeName = parts[0];
            var assemblyName = string.Join( ",", parts.Skip( 1 ) );

            if ( !this.Domain.TryGetLoadedAssembly( assemblyName, out var assembly ) )
            {
                throw new InvalidOperationException( $"Cannot find the assembly '{assemblyName}'." );
            }

            var type = assembly.GetType( typeName, true ).AssertNotNull();

            var plugIn = Activator.CreateInstance( type ).AssertNotNull();

            plugIns.Add( plugIn );
        }

        return plugIns.ToImmutable();
    }

    /// <summary>
    /// Replaces the <see cref="ServiceProvider"/> with a new instance initialized with a specified list of <see cref="PortableExecutableReference"/>.
    /// </summary>
    /// <param name="newReferences"></param>
    internal void SetMetadataReferences( IEnumerable<PortableExecutableReference> newReferences )
    {
        this.ServiceProvider = this.ServiceProvider.Global.Underlying.WithProjectScopedServices( this.ProjectOptions, newReferences );
    }

#pragma warning disable LAMA0821 // Do not expose internal APIs.

#pragma warning restore LAMA0821

    internal CompileTimeDomain Domain => this.ServiceProvider.Global.GetRequiredService<CompileTimeDomain>();

    /// <summary>
    /// Switches the <see cref="MetalamaExecutionContext"/> to a test context for a given <see cref="ICompilation"/>.
    /// This allows compile-time unit tests to use facilities such as <see cref="ExpressionFactory"/>.
    /// </summary>
    /// <param name="compilation"></param>
    /// <param name="description"></param>
    /// <returns></returns>
    [MustDisposeResource]
    public IDisposable WithExecutionContext( ICompilation compilation, string? description = null )
        => UserCodeExecutionContext.WithContext( this.ServiceProvider, (CompilationModel) compilation, description ?? "executing test method" );

    string ITempFileManager.GetTempDirectory( string directory, CleanUpStrategy cleanUpStrategy, string? subdirectory, TempFileVersionScope versionScope )
    {
        if ( directory.StartsWith( TempDirectories.AssemblyLocator, StringComparison.Ordinal ) )
        {
            // For the AssemblyLocator, we use a single directory that is shared by all tests, for every build of the main engine assembly.
            // The reason is performance: this step is too expensive to be performed at each test.
            return this._backstageTempFileManager.GetTempDirectory(
                directory,
                cleanUpStrategy,
                $"{typeof(CompileTimeAspectPipeline).Module.ModuleVersionId}-{subdirectory}" );
        }
        else
        {
            var directoryPath = Path.Combine( this.TestProjectOptions.BaseDirectory, directory, subdirectory ?? "" );

            if ( !Directory.Exists( directoryPath ) )
            {
                Directory.CreateDirectory( directoryPath );
            }

            return directoryPath;
        }
    }

    DateTime IDateTimeProvider.UtcNow => DateTime.UtcNow;

    event Action? IDateTimeProvider.DateChanged
    {
        add => throw new NotSupportedException();
        remove => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets the test name, for diagnostics.
    /// </summary>
    public string? TestName { get; internal set; }

    internal ITestOutputHelper? TestOutputWriter { get; set; }

    protected virtual void Dispose( bool disposing )
    {
        if ( this._isRoot )
        {
            this._applicationExitManager.Dispose();

            this.TestProjectOptions.Dispose();

            if ( this.ServiceProvider.Global.Underlying.TryGetService<CompileTimeDomain>( out var domain ) )
            {
                domain.Dispose();
            }

            // Release all references for GC.
            this.ServiceProvider = ProjectServiceProvider.Empty;

            this._testCancellationTokenSource?.Dispose();
            this._cancellationTokenRegistration?.Dispose();
            this._timer?.Dispose();
        }

        if ( disposing )
        {
            GC.SuppressFinalize( this );
        }
    }

    ~TestContext()
    {
        this.Dispose( false );

        throw new InvalidOperationException( $"The TestContext allocated at the following call stack was not disposed:\n{this._stackTrace}\n------" );
    }

    public void Dispose() => this.Dispose( true );

    IApplicationInfo IApplicationInfoProvider.CurrentApplication => _applicationInfo;
}