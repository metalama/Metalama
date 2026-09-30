// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using BuildMetalama;
using Metalama.Framework.GenerateMetaSyntaxRewriter;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.BillOfMaterials;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Build.Solutions;
using PostSharp.Engineering.BuildTools.ContinuousIntegration.Model;
using PostSharp.Engineering.BuildTools.Docker;
using PostSharp.Engineering.BuildTools.Tools.TeamCity;
using PostSharp.Engineering.BuildTools.Utilities;
using System;
using System.IO;
using MetalamaDependencies = PostSharp.Engineering.BuildTools.Dependencies.Definitions.MetalamaDependencies.V2026_1;

var preferredVersions = MetalamaDependencies.Family.PreferredVersions;

var product = new Product( MetalamaDependencies.Metalama )
{
    BuildTimeout = TimeSpan.FromMinutes( 60 ),
    OverriddenBuildAgentRequirements = new ContainerRequirements( ContainerHostKind.Windows )
    {
        Components =
        [
            // Must match global.json.
            new DotNetComponent( preferredVersions.DotNetSdk.V_10_0, DotNetComponentKind.Sdk ),

            // The runtime is required by all tests.
            // The SDK is required by the Workspace tests.
            new DotNetComponent( preferredVersions.DotNetSdk.V_8_0, DotNetComponentKind.Sdk ),

            // Required by eng and to provide net9.0 targeting pack.
            new DotNetComponent( preferredVersions.DotNetSdk.V_9_0, DotNetComponentKind.Sdk ),

            // Required by some tests.
            new VisualStudioBuildToolsComponent(
                VisualStudioBuildToolsComponentVersion.v17_14_15,
                [
                    // Required to test MSBuild.
                    "Microsoft.Component.MSBuild",
                    "Microsoft.NetCore.Component.SDK",

                    // Required because we target these frameworks.
                    "Microsoft.Net.Component.4.7.2.TargetingPack",
                    "Microsoft.Net.Component.4.7.2.SDK",
                    "Microsoft.Net.Component.4.8.TargetingPack",
                    "Microsoft.Net.Component.4.8.SDK"
                ] ),

            // Required to download test license keys.
            new AzureCliComponent()
        ]
    },
    DotNetSdkVersion = new DotNetSdkVersion( preferredVersions.DotNetSdk.V_10_0 ) { AllowPrerelease = true },
    GenerateNuGetConfig = true,
    MSBuildVersion = new Version( 17, 14 ),
    Solutions =
    [
        new DotNetSolution( "Metalama.Backstage/Metalama.Backstage.sln" ) { SupportsTestCoverage = true, CanFormatCode = true },
        new DotNetSolution( "Metalama.Framework/Metalama.Framework.sln" )
        {
            SolutionFilterPathForInspectCode = "Metalama.Framework/Metalama.Framework.LatestRoslyn.slnf",
            SupportsTestCoverage = true,
            CanFormatCode = true,

            // The tests of the whole solution are run, so that the test projects built against the older supported
            // Roslyn versions are tested as well. See issue #1811.
            TestMethod = BuildMethod.Test,
            FormatExclusions =
            [
                // Test payloads should not be formatted because it would break the test output comparison.
                // In some cases, formatting or redundant keywords may be intentional.
                "src\\tests\\Metalama.Framework.Tests.AspectTests\\Tests\\**\\*",
                "src\\tests\\Metalama.Framework.Tests.LinkerTests\\Tests\\**\\*",
                "src\\tests\\Metalama.Framework.Tests.TemplateTests\\Tests\\**\\*",
                "src\\tests\\Metalama.Extensions.*.AspectTests\\**\\*",
                "**\\*.g.cs",

                // XML formatting seems to be conflicting.
                "**\\*.props", "**\\*.targets", "**\\*.csproj", "**\\*.md", "**\\*.xml", "**\\*.config"
            ]
        },
        // The platform tests are Microsoft.Testing.Platform applications, which the VSTest mode of 'dotnet test' refuses, so the
        // solution is built but not tested. The build packs them into test archives, which the TestAgents run on Linux and
        // macOS, and OnTestCompleted runs them on Windows.
        new DotNetSolution( "Metalama.Framework/Metalama.Framework.PlatformTests.sln" )
        {
            BuildMethod = BuildMethod.Build, TestMethod = BuildMethod.None, ContainsTestApplications = true
        },
        new DotNetSolution( "Metalama.Framework/src/tests/Metalama.Framework.TestApp\\Metalama.Framework.TestApp.sln" )
        {
            IsTestOnly = true, TestMethod = BuildMethod.Build
        },

        // Do at least one test with MSBuild because there can be different errors.
        new MsbuildSolution( "Metalama.Framework/src/tests/Metalama.Framework.TestApp\\Metalama.Framework.TestApp.sln" )
        {
            IsTestOnly = true, TestMethod = BuildMethod.Build
        },
        new ManyDotNetSolutions( "Metalama.Framework/src/Tests/Standalone" ) { IsTestOnly = true },

        // Scenarios that only fail at design time. They are exercised by Metalama.DesignTime.HostSimulator, which
        // hosts Metalama the way an IDE does, instead of by 'dotnet build'.
        new ManyDesignTimeSolutions( "Metalama.Framework/src/Tests/DesignTimeStandalone" ) { IsTestOnly = true },
        new DotNetSolution( "Metalama.Extensions/Metalama.Extensions.sln" ) { CanFormatCode = true, FormatExclusions = ["src\\tests\\*AspectTests\\**\\*"] },
        new DotNetSolution( "Metalama.Patterns/Metalama.Patterns.sln" ) { CanFormatCode = true, FormatExclusions = ["src\\tests\\*AspectTests\\**\\*"] },
        new DotNetSolution( "Metalama.Migration/Metalama.Migration.sln" ) { CanFormatCode = true },
        new DotNetSolution( "Metalama.LinqPad/Metalama.LinqPad.sln" ) { CanFormatCode = true }
    ],
    PublicArtifacts = Pattern.Create(
        "Metalama.Backstage.$(PackageVersion).nupkg",
        "Metalama.Backstage.Commands.$(PackageVersion).nupkg", // Required by SourceLink in Metalama.Framework.
        "Metalama.Backstage.Testing.$(PackageVersion).nupkg",  // Required by SourceLink in Metalama.Framework.
        "Metalama.Backstage.Tools.$(PackageVersion).nupkg",    // Required by Metalama.Testing.AspectTesting via Metalama.Framework.Engine.
        "Metalama.Testing.Hooks.$(PackageVersion).nupkg",      // Required by Metalama.Framework.Engine and Metalama.Patterns.Caching.Backend.
        "Metalama.Framework.$(PackageVersion).nupkg",
        "Metalama.Testing.UnitTesting.$(PackageVersion).nupkg",
        "Metalama.Testing.AspectTesting.$(PackageVersion).nupkg",
        "Metalama.Framework.Redist.$(PackageVersion).nupkg",
        "Metalama.Framework.Sdk.$(PackageVersion).nupkg",
        "Metalama.Framework.Implementation.*.$(PackageVersion).nupkg",
        "Metalama.Framework.Introspection.$(PackageVersion).nupkg",
        "Metalama.Framework.Workspaces.$(PackageVersion).nupkg",
        "Metalama.Tool.$(PackageVersion).nupkg",
        "Metalama.Extensions.DependencyInjection.$(PackageVersion).nupkg",
        "Metalama.Extensions.DependencyInjection.ServiceLocator.$(PackageVersion).nupkg",
        "Metalama.Extensions.Multicast.$(PackageVersion).nupkg",
        "Metalama.Extensions.Metrics.$(PackageVersion).nupkg",
        "Metalama.Extensions.HtmlWriter.$(PackageVersion).nupkg",
        "Metalama.Extensions.DiffEngine.$(PackageVersion).nupkg",
        "Metalama.Migration.$(PackageVersion).nupkg",
        "Metalama.LinqPad.$(PackageVersion).nupkg",
        "Metalama.Patterns.Caching.$(PackageVersion).nupkg",
        "Metalama.Patterns.Caching.Aspects.$(PackageVersion).nupkg",
        "Metalama.Patterns.Caching.Backend.$(PackageVersion).nupkg",
        "Metalama.Patterns.Caching.TestHelpers.$(PackageVersion).nupkg",
        "Metalama.Patterns.Contracts.$(PackageVersion).nupkg",
        "Metalama.Patterns.Memoization.$(PackageVersion).nupkg",
        "Metalama.Patterns.Immutability.$(PackageVersion).nupkg",
        "Metalama.Patterns.Observability.$(PackageVersion).nupkg",
        "Metalama.Patterns.TestHelpers.$(PackageVersion).nupkg",
        "Metalama.Patterns.Wpf.$(PackageVersion).nupkg",
        "Flashtrace.$(PackageVersion).nupkg",
        "Flashtrace.Formatters.$(PackageVersion).nupkg" ),
    PrivateArtifacts = Pattern.Create(
        "Metalama.Framework.Tests.UnitTestHelpers.$(PackageVersion).nupkg",
        "Metalama.Framework.DesignTime.Contracts.$(PackageVersion).nupkg",
        "Metalama.Framework.DesignTime.Rpc.$(PackageVersion).nupkg" ),
    ExportedProperties =
    {
        { "Directory.Packages.props", ["RoslynApiMaxVersion", "RoslynMaxVersion"] }, { "Metalama.Framework\\Directory.Build.props", ["LangMaxVersion"] }
    },
    Configurations = Product.DefaultConfigurations
        .WithValue(
            BuildConfiguration.Debug,
            c => c with
            {
                // The Debug build writes the test archives that the TestAgents run.
                RunsTestArchives = true,
                AdditionalArtifactRules =
                [
                    @"+:%system.teamcity.build.tempDir%/Metalama/ExtractExceptions/**/*=>logs",
                    @"+:%system.teamcity.build.tempDir%/Metalama/Extract/**/.completed=>logs",
                    @"+:%system.teamcity.build.tempDir%/Metalama/CrashReports/**/*=>logs",

                    // Do not upload uncompressed crash reports because they are too big.
                    @"-:%system.teamcity.build.tempDir%/Metalama/CrashReports/**/*.dmp=>logs"
                ]
            } ),
    SupportedProperties = { { "PrepareStubs", "The prepare command generates stub files, instead of actual implementations." } },

    // Bill of Materials
    ProjectUsages =
    [
        new ProjectUsageInfo(
            @"Metalama\.Framework\.(Engine|DesignTime|CompilerExtensions|EditorExtensions|Implementation).*",
            DependentPackageUsageKind.Development,
            ["Metalama.Framework"] ),
        new ProjectUsageInfo(
            @"Metalama\.Framework\.(Workspaces|Introspection|Sdk)",
            DependentPackageUsageKind.Development ),
        new ProjectUsageInfo(
            @"Metalama\.Framework\.Package",
            DependentPackageUsageKind.Development,
            ["Metalama.Framework"] ),
        new ProjectUsageInfo( @"Metalama\.Testing\..*", DependentPackageUsageKind.Development ),
        new ProjectUsageInfo( @"Metalama\.LinqPad", DependentPackageUsageKind.Development ),
        new ProjectUsageInfo(
            @"Metalama\.(SourceTransformer|Framework\.CompileTimeContracts|SystemTypes)",
            DependentPackageUsageKind.Private ),

        // This is the Metalama CLI tool.
        new ProjectUsageInfo( @"^metalama$", DependentPackageUsageKind.Development, ["Metalama CLI"] ),
        new ProjectUsageInfo(
            @"Metalama\.Backstage",
            DependentPackageUsageKind.Development,
            ["Metalama.Framework", "Metalama CLI"] ),
        new ProjectUsageInfo( @"Metalama\.Backstage\.Testing", DependentPackageUsageKind.Private ),

        // We consider test helpers as private dependencies for Metalama.Premium because using them in other scenarios is not officially supported.
        new ProjectUsageInfo( @".*TestHelpers.*", DependentPackageUsageKind.Private )
    ],
    ConsumableDepsFiles = Pattern.Empty.Remove( "**/Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.deps.json" ),
    DependentPackageExclusions =
    [
        new DependentPackageExclusion( "Metalama.Framework", "Current repository." ),
        new DependentPackageExclusion( "Metalama.Extensions", "Current repository." ),
        new DependentPackageExclusion( "Metalama.Backstage", "Current repository." ),
        new DependentPackageExclusion( "Metalama.Compiler", "See notices in <https://github.com/metalama/Metalama.Compiler>." ),

        new DependentPackageExclusion( "Flashtrace", "Current repository." )
    ],
    AddWslSupport = true,

    // The Linux image of the platform tests. The product builds on Windows only, so this image runs the test archives and
    // builds nothing: it needs PowerShell, which RunTests.ps1 requires, and the runtime of the tests, which the .NET 8 SDK
    // includes.
    AdditionalDockerfiles =
    [
        new AdditionalDockerfile( "linux-x64", [] )
        {
            Requirements = new ContainerRequirements( ContainerHostKind.Linux )
            {
                OperatingSystem = ContainerOperatingSystem.Linux,
                Components =
                [
                    new PowershellComponent( ContainerArchitecture.X64 ),
                    new DotNetComponent( preferredVersions.DotNetSdk.V_8_0, DotNetComponentKind.Sdk )
                ]
            }
        }
    ],

    // The agents that run the test archives of the platform tests (Metalama.Framework.PlatformTests). Linux runs them in a
    // container, and macOS on the agent, because no container engine provides a macOS container.
    TestAgents =
    [
        new TestAgent( "linux-x64", "PlatformTestLinuxX64", "Platform Tests Linux x64", CreateLinuxContainerHostRequirements() )
        {
            Dockerfile = "eng/docker/linux-x64-build.Dockerfile", ProjectFolder = "Platform Tests"
        },
        new TestAgent(
            "osx-arm64",
            "PlatformTestMacOsArm64",
            "Platform Tests macOS ARM64",
            new BuildAgentRequirements(
                new BuildAgentRequirement( "teamcity.agent.jvm.os.name", "Mac OS X" ),
                new BuildAgentRequirement( "teamcity.agent.jvm.os.arch", "aarch64" ) ) ) { ProjectFolder = "Platform Tests" }
    ],
    AdditionalCiBuildConfigurations = DockerTestsAdditionalCiBuildConfiguration.WithCompositeConfiguration(
        CreateDockerTestConfiguration( DockerTestPlatform.WindowsX64, "Windows x64" ),
        CreateDockerTestConfiguration( DockerTestPlatform.LinuxX64, "Linux x64" ) )
};

product.PrepareCompleted += OnPrepareCompleted;
product.TestCompleted += OnTestCompleted;

return new EngineeringApp( product ).Run( args );

static void OnPrepareCompleted( PrepareCompletedEventArgs args )
{
    if ( !TestLicenseKeyDownloader.Download( args.Context, args.Settings ) )
    {
        if ( args.Context.IsContinuousIntegrationBuild )
        {
            args.IsFailed = true;

            return;
        }
        else
        {
            args.Context.Console.WriteWarning( "Ignoring errors while downloading test license keys." );
        }
    }

    args.Context.Console.WriteHeading( "Generating code" );

    args.Context.Console.WriteHeading( "Generating code" );

    var srcDirectory = Path.Combine( args.Context.RepoDirectory, "Metalama.Framework" );

    GenerateMetaSyntaxRewriter.Generate( srcDirectory );
}

// Runs the platform tests (Metalama.Framework.PlatformTests) on Windows, for .NET Framework and for .NET.
//
// The platform tests are Microsoft.Testing.Platform applications, which the VSTest mode of 'dotnet test' refuses, so the
// test step does not run them. This handler runs the executables of the build instead, writes their TRX reports to the test
// results directory, and imports the reports into TeamCity. The TestAgents run the same tests on Linux and macOS.
//
// The test command does not read BuildCompletedEventArgs.IsFailed after this event, so the handler also throws
// an exception when a test run fails, which fails the command.
static void OnTestCompleted( BuildCompletedEventArgs args )
{
    const string projectName = "Metalama.Framework.PlatformTests";

    var context = args.Context;
    var msbuildConfiguration = context.Product.DependencyDefinition.MSBuildConfiguration[args.Settings.BuildConfiguration];

    var outputDirectory = Path.Combine(
        context.RepoDirectory,
        "Metalama.Framework",
        "src",
        "tests",
        projectName,
        "bin",
        msbuildConfiguration );

    var resultsDirectory = Path.Combine( context.RepoDirectory, context.Product.TestResultsDirectory, projectName );

    context.Console.WriteHeading( "Running the platform tests" );

    var runs = new (string TargetFramework, string FileName, string EntryArgument)[]
    {
        ("net48", Path.Combine( outputDirectory, "net48", $"{projectName}.exe" ), ""),
        ("net8.0", "dotnet", $"\"{Path.Combine( outputDirectory, "net8.0", $"{projectName}.dll" )}\" ")
    };

    var success = true;

    foreach ( var run in runs )
    {
        var runResultsDirectory = Path.Combine( resultsDirectory, run.TargetFramework );

        if ( !ToolInvocationHelper.InvokeTool(
                context.Console,
                run.FileName,
                $"{run.EntryArgument}--report-trx --results-directory \"{runResultsDirectory}\"",
                Path.Combine( outputDirectory, run.TargetFramework ) ) )
        {
            success = false;
        }

        if ( context.IsContinuousIntegrationBuild && Directory.Exists( runResultsDirectory ) )
        {
            foreach ( var report in Directory.EnumerateFiles( runResultsDirectory, "*.trx", SearchOption.AllDirectories ) )
            {
                TeamCityHelper.SendImportDataMessage( "mstest", report.Replace( Path.DirectorySeparatorChar, '/' ), projectName, false );
            }
        }
    }

    if ( !success )
    {
        args.IsFailed = true;

        throw new InvalidOperationException( "The platform tests failed on Windows. See the output above." );
    }
}

// Creates the requirements of the Linux agents that run the test archives in a container.
//
// The requirements that PostSharp.Engineering derives for a Linux container host ask for an env.BuildAgentType that
// the Linux agents do not publish, so no agent would be compatible. The operating system and the architecture are what the
// agents publish.
static ContainerHostRequirements CreateLinuxContainerHostRequirements()
    => new ContainerHostRequirements( ContainerHostKind.Linux ) with
    {
        Items =
        [
            new BuildAgentRequirement( "teamcity.agent.jvm.os.name", "Linux" ),
            new BuildAgentRequirement( "teamcity.agent.jvm.os.arch", "amd64" )
        ]
    };

/// <summary>
/// Creates the configuration that runs the Docker-based tests of one platform.
/// </summary>
/// <remarks>
/// The agent requirements are derived from the platform rather than stated here, and are deliberately not a
/// <see cref="ContainerHostRequirements"/>. That is what the previous configurations used, and it makes the
/// generator wrap the step in a container of the product image, so a test needing a container of its own would
/// have to nest one engine inside another. The launcher runs on the agent instead.
/// </remarks>
static DockerTestsAdditionalCiBuildConfiguration CreateDockerTestConfiguration( DockerTestPlatform platform, string title )
    => new(
        $"DockerTests{platform}",
        $"Docker Tests ({title})",
        platform,
        "Metalama.Framework/src/tests/docker" )
    {
        BuildSnapshotDependency = BuildConfiguration.Debug
    };
