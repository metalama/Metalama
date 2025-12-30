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

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using JetBrains.Profiler.Api;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Tests.Benchmarks;
using Microsoft.Build.Locator;
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

    var sw = Stopwatch.StartNew();
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
    .AddJob(
        Job.Default
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

        return string.IsNullOrEmpty( branchName ) ? "unknown" : branchName.Replace( "/", "-", StringComparison.Ordinal );
    }
    catch
    {
        return "unknown";
    }
}