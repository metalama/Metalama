// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// TemplatingCodeValidator Benchmark
// Validates the entire nopCommerce.Core project to measure validation performance.
//
// Usage:
//   dotnet run -c Release
//   dotnet run -c Release -- --test  (for quick test without BenchmarkDotNet)
//   dotnet run -c Release -- --test --dottrace  (run under dotTrace profiler with data collection)

#pragma warning disable CA1822

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using JetBrains.Profiler.Api;
using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Testing.UnitTesting;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
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
    await benchmarks.Setup();

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
    benchmarks.Cleanup();
    return;
}

BenchmarkRunner.Run<TemplatingCodeValidatorBenchmarks>();
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
    private const string NopCommerceCoreProject = @"C:\src\Metalama-2026.0\Metalama.Tests.NopCommerce\src\Libraries\Nop.Core\Nop.Core.csproj";

    private TestContext? _testContext;
    private Compilation? _compilation;
    private MSBuildWorkspace? _workspace;

    [GlobalSetup]
    public async Task Setup()
    {
        Console.WriteLine( "Setting up benchmark..." );

        // Initialize Metalama services (must be done in spawned process)
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

        // Create test context with Metalama services
        _testContext = new TestContext( new TestContextOptions() );

        // Load nopCommerce.Core project using MSBuildWorkspace
        Console.WriteLine( $"Loading project: {NopCommerceCoreProject}" );

        _workspace = MSBuildWorkspace.Create();
        _workspace.WorkspaceFailed += ( sender, args ) =>
        {
            if ( args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure )
            {
                Console.WriteLine( $"Workspace error: {args.Diagnostic.Message}" );
            }
        };

        var project = await _workspace.OpenProjectAsync( NopCommerceCoreProject );

        Console.WriteLine( $"Loaded project: {project.Name}" );
        Console.WriteLine( $"Documents: {project.Documents.Count()}" );

        var roslynCompilation = await project.GetCompilationAsync();

        if ( roslynCompilation == null )
        {
            throw new InvalidOperationException( "Failed to get compilation" );
        }

        // Add Metalama references to the compilation so TemplatingCodeValidator can find the types
        var metalamaReferences = _testContext.GetMetadataReferences();
        _compilation = roslynCompilation.AddReferences( metalamaReferences );

        Console.WriteLine( $"Created compilation with {_compilation.SyntaxTrees.Count()} trees and {_compilation.References.Count()} references" );
        Console.WriteLine( "Setup complete!" );
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _workspace?.Dispose();
        _testContext?.Dispose();
    }

    [Benchmark]
    public async Task<int> ValidateAllSyntaxTrees()
    {
        var diagnosticCount = 0;

        await TemplatingCodeValidator.ValidateAsync(
            _testContext!.ServiceProvider,
            _compilation!,
            _ => Interlocked.Increment( ref diagnosticCount ),
            CancellationToken.None );

        return diagnosticCount;
    }
}

internal sealed class BenchmarkApplicationInfo : ApplicationInfoBase
{
    public BenchmarkApplicationInfo() : base( typeof(BenchmarkApplicationInfo).Assembly ) { }

    public override string Name => "Metalama.Framework.Tests.Benchmarks";

    public override bool ShouldCreateLocalCrashReports => false;
}
