using BenchmarkDotNet.Attributes;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Framework.Engine.Observers;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Metalama.Framework.Tests.Benchmarks;

[MemoryDiagnoser]
public sealed class TemplatingCodeValidatorBenchmarks : IDisposable
{
    private const string _nopCommerceSolutionEnvVar = "METALAMA_BENCHMARK_NOPCOMMERCE_SOLUTION";

    private TestContext? _testContext;
    private string? _nopCommerceSolution;
    private Compilation[]? _compilations;
    private MSBuildWorkspace? _workspace;
    private bool _backstageInitialized;
    private TemplatingCodeValidatorObserver? _observer;

    [GlobalSetup]
    public void GlobalSetup()
    {
        this.GlobalSetupAsync().GetAwaiter().GetResult();
    }

    private async Task GlobalSetupAsync()
    {
        Console.WriteLine( "Setting up benchmark (global)..." );

        // Get solution path from environment variable
        this._nopCommerceSolution = Environment.GetEnvironmentVariable( _nopCommerceSolutionEnvVar );

        if ( string.IsNullOrEmpty( this._nopCommerceSolution ) )
        {
            throw new InvalidOperationException(
                $"Environment variable '{_nopCommerceSolutionEnvVar}' is not defined. " +
                "Set it to the path of the nopCommerce solution file (e.g., C:\\src\\nopCommerce\\src\\NopCommerce.sln)." );
        }

        if ( !File.Exists( this._nopCommerceSolution ) )
        {
            throw new InvalidOperationException(
                $"Solution file '{this._nopCommerceSolution}' does not exist. " +
                $"Check the value of the '{_nopCommerceSolutionEnvVar}' environment variable." );
        }

        // Initialize Metalama services (must be done once)
        if ( !this._backstageInitialized )
        {
            BackstageServiceFactoryInitializer.Initialize(
                new BackstageInitializationOptions( new BenchmarkApplicationInfo() )
                {
                    AddSupportServices = true,
                    AddLicensing = false,
                    LicensingOptions = LicensingInitializationOptions.ForTest(
                        license =>
                        {
                            license.Product = LicenseProduct.MetalamaProfessional;
                            license.LicenseType = LicenseType.Test;
                            license.SubscriptionEndDate = DateTime.MaxValue;
                        } )
                } );

            this._backstageInitialized = true;
        }

        // Load nopCommerce solution using MSBuildWorkspace
        Console.WriteLine( $"Loading solution: {this._nopCommerceSolution}" );

        var workspaceFailures = new List<string>();
        this._workspace = MSBuildWorkspace.Create();

        this._workspace.RegisterWorkspaceFailedHandler(
            args =>
            {
                if ( args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure )
                {
                    workspaceFailures.Add( args.Diagnostic.Message );
                    Console.WriteLine( $"Workspace error: {args.Diagnostic.Message}" );
                }
            } );

        var solution = await this._workspace.OpenSolutionAsync( this._nopCommerceSolution );

        if ( workspaceFailures.Count > 0 )
        {
            throw new InvalidOperationException(
                $"Failed to load solution due to {workspaceFailures.Count} workspace error(s):\n" +
                string.Join( "\n", workspaceFailures.Take( 5 ) ) +
                (workspaceFailures.Count > 5 ? $"\n... and {workspaceFailures.Count - 5} more" : "") );
        }

        var projects = solution.Projects.ToArray();
        Console.WriteLine( $"Loaded solution with {projects.Length} projects" );

        // Get Metalama references using a temporary TestContext
        IReadOnlyList<PortableExecutableReference> metalamaReferences;

        using ( var tempContext = new TestContext( new TestContextOptions() ) )
        {
            metalamaReferences = tempContext.GetMetadataReferences();
        }

        var compilations = new Compilation[projects.Length];
        var errors = new Exception?[projects.Length];

        // Load and validate all compilations in parallel
        await Parallel.ForAsync(
            0,
            projects.Length,
            async ( i, _ ) =>
            {
                var project = projects[i];
                Console.WriteLine( $"  Loading project: {project.Name} ({project.Documents.Count()} documents)" );

                try
                {
                    var roslynCompilation = await project.GetCompilationAsync();

                    if ( roslynCompilation == null )
                    {
                        errors[i] = new InvalidOperationException( $"Failed to get compilation for project {project.Name}" );

                        return;
                    }

                    // Check for compilation errors
                    var compilationErrors = roslynCompilation.GetDiagnostics()
                        .Where( d => d.Severity == DiagnosticSeverity.Error )
                        .ToList();

                    if ( compilationErrors.Count > 0 )
                    {
                        errors[i] = new InvalidOperationException(
                            $"Project {project.Name} has {compilationErrors.Count} error(s):\n" +
                            string.Join( "\n", compilationErrors.Take( 10 ).Select( d => d.ToString() ) ) +
                            (compilationErrors.Count > 10 ? $"\n... and {compilationErrors.Count - 10} more" : "") );

                        return;
                    }

                    // Add Metalama references - required by TemplatingCodeValidator to resolve Metalama types
                    compilations[i] = roslynCompilation.AddReferences( metalamaReferences );
                }
                catch ( Exception ex )
                {
                    errors[i] = ex;
                }
            } );

        // Check for errors
        var firstError = errors.FirstOrDefault( e => e != null );

        if ( firstError != null )
        {
            throw firstError;
        }

        this._compilations = compilations;
        var totalTrees = this._compilations.Sum( c => c.SyntaxTrees.Count() );

        Console.WriteLine( $"Created {this._compilations.Length} compilations with {totalTrees} total syntax trees" );
        Console.WriteLine( "Setup complete!" );
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        this._workspace?.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Invalidate all static WeakCache instances to ensure consistent measurements
        WeakCache.Invalidate();

        // Create and reset the observer
        this._observer = new TemplatingCodeValidatorObserver();

        // Create fresh TestContext for each iteration to avoid caching effects
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.ProjectServices.Add<IConcurrentTaskRunner>( _ => new ConcurrentTaskRunner() );
        additionalServices.GlobalServices.Add<ITemplatingCodeValidatorObserver>( _ => this._observer! );
        this._testContext = new TestContext( new TestContextOptions(), additionalServices );
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        // Print observer metrics
        this._observer?.PrintMetrics();

        this._testContext?.Dispose();
        this._testContext = null;
        this._observer = null;
    }

    [Benchmark]
    public async Task<int> ValidateAllSyntaxTrees()
    {
        var diagnosticCount = 0;

        await Parallel.ForEachAsync(
            this._compilations!,
            async ( compilation, _ ) =>
            {
                await TemplatingCodeValidator.ValidateAsync(
                    this._testContext!.ServiceProvider,
                    compilation,
                    _ => Interlocked.Increment( ref diagnosticCount ),
                    CancellationToken.None );
            } );

        return diagnosticCount;
    }

    public void Dispose()
    {
        this._testContext?.Dispose();
        this._workspace?.Dispose();
    }
}