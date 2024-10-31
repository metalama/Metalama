// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Maintenance;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
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
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit.Abstractions;

namespace Metalama.Testing.UnitTesting;

/// <summary>
/// A context in which a Metalama unit test can run, configured with most required Metalama services and optionally some mocks.
/// </summary>
[PublicAPI]
public class TestContext : IDisposable, ITempFileManager, IApplicationInfoProvider, IDateTimeProvider
{
    private static readonly IApplicationInfo _applicationInfo = new TestApiApplicationInfo();
    private readonly ITempFileManager _backstageTempFileManager;
    private readonly bool _isRoot;
    private readonly IDisposable? _throttlingHandle;
    private readonly StackTrace _stackTrace = new();

    // We keep the domain in a strongbox so that we share domain instances with TestContext instances created with With* method.
    private readonly StrongBox<CompileTimeDomain?> _domain;

    private readonly CancellationTokenSource? _timeoutCancellationTokenSource;

    internal TestProjectOptions ProjectOptions { get; }

    /// <summary>
    /// Gets the directory that was specifically created for the current test and where all specific files should be stored.
    /// </summary>
    public string BaseDirectory => this.ProjectOptions.BaseDirectory;

    /// <summary>
    /// Gets the <see cref="ProjectServiceProvider"/> for the current context.
    /// </summary>
    public ProjectServiceProvider ServiceProvider { get; private set; }

    /// <summary>
    /// Gets a <see cref="CancellationToken"/> used to cancel the test in case of timeout. The timeout period is defined
    /// by the <see cref="TestContextOptions.Timeout"/> option.
    /// </summary>
    public CancellationToken CancellationToken => this._timeoutCancellationTokenSource?.Token ?? default;

    // ReSharper disable once RedundantOverload.Global

    /// <summary>
    /// Initializes a new instance of the <see cref="TestContext"/> class. Tests typically
    /// do not call this constructor directly, but instead the <see cref="UnitTestClass.CreateTestContext(IAdditionalServiceCollection,string?,string?)"/>
    /// method.
    /// </summary>
    public TestContext( TestContextOptions contextOptions ) : this( contextOptions, null ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestContext"/> class and specify an optional <see cref="IAdditionalServiceCollection"/>. Tests typically
    /// do not call this constructor directly, but instead the <see cref="UnitTestClass.CreateTestContext(IAdditionalServiceCollection,string?,string?)"/>
    /// method.
    /// </summary>
    public TestContext(
        TestContextOptions contextOptions,
        IAdditionalServiceCollection? additionalServices = null )
    {
        if ( !Debugger.IsAttached )
        {
            this._timeoutCancellationTokenSource = new CancellationTokenSource( contextOptions.Timeout );
        }
        else
        {
            // We don't cancel tests when a debugger is attached because it's then normal that a test runs during a long time.
        }

        this._throttlingHandle = TestThrottlingHelper.StartTest( contextOptions.RequiresExclusivity );
        this._domain = new StrongBox<CompileTimeDomain?>();
        this._isRoot = true;

        this.ProjectOptions = new TestProjectOptions( contextOptions );

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

            var licenseConsumptionService = BackstageServiceFactory.ServiceProvider.GetRequiredBackstageService<ILicenseConsumptionService>();

            backstageServices = backstageServices.WithService( licenseConsumptionService );
            backstageServices = backstageServices.WithService( new InMemoryConfigurationManager( backstageServices ), true );

            var typedAdditionalServices = (AdditionalServiceCollection?) additionalServices ?? new AdditionalServiceCollection();
            typedAdditionalServices.GlobalServices.Add( sp => sp.WithServiceConditional<IGlobalOptions>( _ => new TestGlobalOptions() ) );

            typedAdditionalServices.GlobalServices.Add(
                sp => sp.WithService<IProjectOptionsFactory>( _ => new TestProjectOptionsFactory( this.ProjectOptions ) ) );

            backstageServices = typedAdditionalServices.BackstageServices.Build( backstageServices );

            var serviceProvider = ServiceProviderFactory.GetServiceProvider( backstageServices, typedAdditionalServices );

            serviceProvider = serviceProvider
                .WithService( this.ProjectOptions.DomainObserver );

            var randomGeneratorService = contextOptions.RunnerServiceProvider.GetService<IRandomNumberProvider>() ?? new RandomNumberProvider();
            serviceProvider = serviceProvider.WithService( randomGeneratorService );

            this.ServiceProvider = serviceProvider
                .WithProjectScopedServices( this.ProjectOptions, contextOptions.References );
        }
        catch
        {
            // Avoid a misleading exception thrown by the finalizer.
            GC.SuppressFinalize( this );

            throw;
        }
    }

    /// <summary>
    /// Replaces the <see cref="ServiceProvider"/> with a new instance initialized with a specified list of <see cref="PortableExecutableReference"/>.
    /// </summary>
    /// <param name="newReferences"></param>
    internal void SetMetadataReferences( IEnumerable<PortableExecutableReference> newReferences )
    {
        this.ServiceProvider = this.ServiceProvider.Global.Underlying.WithProjectScopedServices( this.ProjectOptions, newReferences );
    }

    /// <summary>
    /// Creates an <see cref="ICompilation"/> made of a single source file.
    /// </summary>
    /// <param name="code">Source code.</param>
    /// <param name="dependentCode">Source code of another assembly added as a reference to the
    /// returned assembly. Optional.</param>
    /// <param name="ignoreErrors">Determines whether compilation errors should be ignored.
    /// Optional.</param>
    /// <param name="additionalReferences">Additional set of <see cref="MetadataReference"/>
    /// added to the compilation.</param>
    /// <param name="name">Name of the assembly.</param>
    /// <param name="addMetalamaReferences">Determines if Metalama assemblies should be added
    /// as references to the compilation. Optional. The default value is <c>true</c>.</param>
    /// <returns></returns>
    public ICompilation CreateCompilation(
        string code,
        string? dependentCode = null,
        bool ignoreErrors = false,
        IEnumerable<MetadataReference>? additionalReferences = null,
        string? name = null,
        bool addMetalamaReferences = true )
        => this.CreateCompilation(
            new Dictionary<string, string> { { "test.cs", code } },
            dependentCode,
            ignoreErrors,
            additionalReferences,
            name,
            addMetalamaReferences );

    /// <summary>
    /// Creates an <see cref="ICompilation"/> made of several source files.
    /// </summary>
    /// <param name="code">Source code. The key of the dictionary item is the file name, the value is
    /// the source code.</param>
    /// <param name="dependentCode">Source code of another assembly added as a reference to the
    /// returned assembly. Optional.</param>
    /// <param name="ignoreErrors">Determines whether compilation errors should be ignored.
    /// Optional.</param>
    /// <param name="additionalReferences">Additional set of <see cref="MetadataReference"/>
    /// added to the compilation.</param>
    /// <param name="name">Name of the assembly.</param>
    /// <param name="addMetalamaReferences">Determines if Metalama assemblies should be added
    /// as references to the compilation. Optional. The default value is <c>true</c>.</param>
    public ICompilation CreateCompilation(
        IReadOnlyDictionary<string, string> code,
        string? dependentCode = null,
        bool ignoreErrors = false,
        IEnumerable<MetadataReference>? additionalReferences = null,
        string? name = null,
        bool addMetalamaReferences = true )
    {
        var allAdditionalReferences = ImmutableArray<MetadataReference>.Empty;

        if ( !this.ProjectOptions.AdditionalAssemblies.IsDefaultOrEmpty )
        {
            allAdditionalReferences = allAdditionalReferences.AddRange(
                this.ProjectOptions.AdditionalAssemblies.Select( a => MetadataReference.CreateFromFile( a.Location ) ) );
        }

        if ( additionalReferences != null )
        {
            allAdditionalReferences = allAdditionalReferences.AddRange( additionalReferences );
        }

        var roslynCompilation = TestCompilationFactory.CreateCSharpCompilation(
            code,
            dependentCode,
            ignoreErrors,
            allAdditionalReferences,
            name,
            addMetalamaReferences );

        return CompilationModel.CreateInitialInstance(
            new ProjectModel( roslynCompilation, this.ServiceProvider ),
            roslynCompilation );
    }

    /// <summary>
    /// Creates an <see cref="ICompilation"/> from a <see cref="Compilation"/>.
    /// </summary>
    public ICompilation CreateCompilation( Compilation compilation )
        => CompilationModel.CreateInitialInstance(
            new ProjectModel( compilation, this.ServiceProvider ),
            compilation );

    internal CompilationModel CreateCompilationModel(
        string code,
        string? dependentCode = null,
        bool ignoreErrors = false,
        IEnumerable<MetadataReference>? additionalReferences = null,
        string? name = null,
        bool addMetalamaReferences = true )
        => (CompilationModel) this.CreateCompilation( code, dependentCode, ignoreErrors, additionalReferences, name, addMetalamaReferences );

    internal CompilationModel CreateCompilationModel(
        IReadOnlyDictionary<string, string> code,
        string? dependentCode = null,
        bool ignoreErrors = false,
        IEnumerable<MetadataReference>? additionalReferences = null,
        string? name = null,
        bool addMetalamaReferences = true )
        => (CompilationModel) this.CreateCompilation( code, dependentCode, ignoreErrors, additionalReferences, name, addMetalamaReferences );

    internal CompilationModel CreateCompilationModel( Compilation compilation ) => (CompilationModel) this.CreateCompilation( compilation );

    private CompileTimeDomain CreateDomain()
#if NET5_0_OR_GREATER
    {
        var domain = new UnloadableCompileTimeDomain( this.ServiceProvider.Global );
        domain.UnloadTimeout += DiagnosticsHelper.CaptureDotMemoryDumpAndThrow;

        return domain;
    }
#else
        => new( this.ServiceProvider.Global );
#endif

    internal CompileTimeDomain Domain => this._domain.Value ??= this.CreateDomain();

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
                typeof(CompileTimeAspectPipeline).Module.ModuleVersionId.ToString() );
        }
        else
        {
            var directoryPath = Path.Combine( this.ProjectOptions.BaseDirectory, directory, subdirectory ?? "" );

            if ( !Directory.Exists( directoryPath ) )
            {
                Directory.CreateDirectory( directoryPath );
            }

            return directoryPath;
        }
    }

    DateTime IDateTimeProvider.UtcNow => DateTime.UtcNow;
    
    /// <summary>
    /// Gets the test name, for diagnostics.
    /// </summary>
    public string? TestName { get; internal set; }
    
    internal ITestOutputHelper? TestOutputWriter { get; set; }

    protected virtual void Dispose( bool disposing )
    {
        if ( this._isRoot )
        {
            this.ProjectOptions.Dispose();
            this._domain.Value?.Dispose();
            this._timeoutCancellationTokenSource?.Dispose();
            this._throttlingHandle?.Dispose();
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