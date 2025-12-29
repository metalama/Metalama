// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// TemplatingCodeValidator Benchmark
// Validates all projects in the nopCommerce solution to measure validation performance.
//
// Usage:
//   dotnet run -c Release
//   dotnet run -c Release -- --test  (for quick test without BenchmarkDotNet)
//   dotnet run -c Release -- --test --dottrace  (run under dotTrace profiler with data collection)

#pragma warning disable CA1822

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using JetBrains.Profiler.Api;
using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Testing.UnitTesting;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Immutable;
using System.Diagnostics;

// Register MSBuild before anything else
MSBuildLocator.RegisterDefaults();

// Quick test mode: run with --test to verify setup works
if ( args.Contains( "--test" ) )
{
    var useDotTrace = args.Contains( "--dottrace" );

    Console.WriteLine( "Running quick test mode..." );

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

    var benchmarks = new TemplatingCodeValidatorBenchmarks();
    benchmarks.GlobalSetup();
    benchmarks.IterationSetup();

    if ( useDotTrace )
    {
        Console.WriteLine( "Starting dotTrace data collection..." );
        MeasureProfiler.StartCollectingData();
    }

    var sw = System.Diagnostics.Stopwatch.StartNew();
    var count = await benchmarks.ValidateAllSyntaxTrees();
    sw.Stop();

    if ( useDotTrace )
    {
        var branchName = GetGitBranchName();
        var snapshotName = $"TemplatingCodeValidator-{branchName}";
        MeasureProfiler.SaveData( snapshotName );
        Console.WriteLine( $"Saved dotTrace snapshot: {snapshotName}" );
    }

    Console.WriteLine( $"Validation completed in {sw.ElapsedMilliseconds} ms, found {count} diagnostics" );
    benchmarks.IterationCleanup();
    benchmarks.GlobalCleanup();
    return;
}

// Use InProcessEmitToolchain to avoid BenchmarkDotNet's build issues with .NET SDK 10.0
// (SDK 10.0 doesn't recognize /p: syntax that BenchmarkDotNet uses)
// Configure for long-running benchmarks with 5% acceptable variance
var config = DefaultConfig.Instance
    .AddJob( Job.Default
        .WithToolchain( new InProcessEmitToolchain( TimeSpan.FromMinutes( 30 ), logOutput: true ) )
        .WithMaxRelativeError( 0.05 ) );

BenchmarkRunner.Run<TemplatingCodeValidatorBenchmarks>( config );
return;

static string GetGitBranchName()
{
    try
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --abbrev-ref HEAD",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var branchName = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        return string.IsNullOrEmpty( branchName ) ? "unknown" : branchName.Replace( "/", "-" );
    }
    catch
    {
        return "unknown";
    }
}

[MemoryDiagnoser]
public class TemplatingCodeValidatorBenchmarks
{
    private const string NopCommerceSolution = @"C:\src\Metalama-2025.1\nopCommerce-benchmark\src\NopCommerce.sln";

    private TestContext? _testContext;
    private Compilation[]? _compilations;
    private MSBuildWorkspace? _workspace;
    private bool _backstageInitialized;

    [GlobalSetup]
    public void GlobalSetup() { GlobalSetupAsync().GetAwaiter().GetResult(); } private async Task GlobalSetupAsync()
    {
        Console.WriteLine( "Setting up benchmark (global)..." );

        // Initialize Metalama services (must be done once)
        if ( !_backstageInitialized )
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

            _backstageInitialized = true;
        }

        // Load nopCommerce solution using MSBuildWorkspace
        Console.WriteLine( $"Loading solution: {NopCommerceSolution}" );

        var workspaceFailures = new List<string>();
        _workspace = MSBuildWorkspace.Create();
        _workspace.WorkspaceFailed += ( sender, args ) =>
        {
            if ( args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure )
            {
                workspaceFailures.Add( args.Diagnostic.Message );
                Console.WriteLine( $"Workspace error: {args.Diagnostic.Message}" );
            }
        };

        var solution = await _workspace.OpenSolutionAsync( NopCommerceSolution );

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

        _compilations = compilations;
        var totalTrees = _compilations.Sum( c => c.SyntaxTrees.Count() );

        Console.WriteLine( $"Created {_compilations.Length} compilations with {totalTrees} total syntax trees" );
        Console.WriteLine( "Setup complete!" );
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _workspace?.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Invalidate all static WeakCache instances to ensure consistent measurements
        WeakCache.Invalidate();

        // Create fresh TestContext for each iteration to avoid caching effects
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.ProjectServices.Add<IConcurrentTaskRunner>( _ => new ConcurrentTaskRunner() );
        _testContext = new TestContext( new TestContextOptions(), additionalServices );
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _testContext?.Dispose();
        _testContext = null;
    }

    [Benchmark]
    public async Task<int> ValidateAllSyntaxTrees()
    {
        var diagnosticCount = 0;

        await Parallel.ForEachAsync(
            _compilations!,
            async ( compilation, _ ) =>
            {
                await TemplatingCodeValidator.ValidateAsync(
                    _testContext!.ServiceProvider,
                    compilation,
                    _ => Interlocked.Increment( ref diagnosticCount ),
                    CancellationToken.None );
            } );

        return diagnosticCount;
    }
}

internal sealed class BenchmarkApplicationInfo : ApplicationInfoBase
{
    public BenchmarkApplicationInfo() : base( typeof(BenchmarkApplicationInfo).Assembly ) { }

    public override string Name => "Metalama.Framework.Tests.Benchmarks";

    public override bool ShouldCreateLocalCrashReports => false;
}
