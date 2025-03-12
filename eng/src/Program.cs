// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.IO;
using Metalama.Framework.GenerateMetaSyntaxRewriter;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Build.Solutions;
using PostSharp.Engineering.BuildTools.Dependencies.Definitions;
using PostSharp.Engineering.BuildTools.Dependencies.Model;
using PostSharp.Engineering.BuildTools.Utilities;
using MetalamaDependencies = PostSharp.Engineering.BuildTools.Dependencies.Definitions.MetalamaDependencies.V2025_1;

var product = new Product( MetalamaDependencies.Metalama )
{
    Solutions =
    [
        new DotNetSolution( "Metalama.Backstage/Metalama.Backstage.sln" ) { SupportsTestCoverage = true, CanFormatCode = true },
        new DotNetSolution( "Metalama.Framework/Metalama.Framework.sln" )
        {
            SolutionFilterPathForInspectCode = "Metalama.Framework.LatestRoslyn.slnf",
            SupportsTestCoverage = true,
            CanFormatCode = true,

            // We don't run the tests for the whole solution because they are too slow and redundant. See #34277.
            TestMethod = BuildMethod.None,
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
        new DotNetSolution( "Metalama.Framework/Metalama.Framework.LatestRoslyn.slnf" )
        {
            SupportsTestCoverage = false, CanFormatCode = false, IsTestOnly = true
        },
        new DotNetSolution( "Metalama.Framework/src/tests/Metalama.Framework.TestApp\\Metalama.Framework.TestApp.sln" )
        {
            IsTestOnly = true, TestMethod = BuildMethod.Build
        },
        new ManyDotNetSolutions( "Metalama.Framework/src/Tests/Standalone" ) { IsTestOnly = true },
        new DotNetSolution( "Metalama.Extensions/Metalama.Extensions.sln" ) 
        { 
            CanFormatCode = true,
            FormatExclusions = ["src\\tests\\*AspectTests\\**\\*"],
        },
        new DotNetSolution( "Metalama.Migration/Metalama.Migration.sln" ) { CanFormatCode = true },
        new DotNetSolution( "Metalama.LinqPad/Metalama.LinqPad.sln" ) { CanFormatCode = true },
        new DotNetSolution( "Metalama.Patterns/Metalama.Patterns.sln" )
        { 
            CanFormatCode = true,
            FormatExclusions = ["src\\tests\\*AspectTests\\**\\*"]
        }
    ],
    PublicArtifacts = Pattern.Create(
        "Metalama.Backstage.$(PackageVersion).nupkg",
        "Metalama.Backstage.Commands.$(PackageVersion).nupkg", // Required by SourceLink in Metalama.Framework.
        "Metalama.Backstage.Testing.$(PackageVersion).nupkg", // Required by SourceLink in Metalama.Framework.
        "Metalama.Backstage.Tools.$(PackageVersion).nupkg", // Required by Metalama.Testing.AspectTesting via Metalama.Framework.Engine.

        "Metalama.Framework.$(PackageVersion).nupkg",
        "Metalama.Testing.UnitTesting.$(PackageVersion).nupkg",
        "Metalama.Testing.AspectTesting.$(PackageVersion).nupkg",
        "Metalama.Framework.Redist.$(PackageVersion).nupkg",
        "Metalama.Framework.Sdk.$(PackageVersion).nupkg",
        "Metalama.Framework.Engine.$(PackageVersion).nupkg",
        "Metalama.Framework.CompileTimeContracts.$(PackageVersion).nupkg",
        "Metalama.Framework.Introspection.$(PackageVersion).nupkg",
        "Metalama.Framework.Workspaces.$(PackageVersion).nupkg",
        "Metalama.Tool.$(PackageVersion).nupkg",

        "Metalama.Extensions.DependencyInjection.$(PackageVersion).nupkg",
        "Metalama.Extensions.DependencyInjection.ServiceLocator.$(PackageVersion).nupkg",
        "Metalama.Extensions.Multicast.$(PackageVersion).nupkg",
        "Metalama.Extensions.Metrics.$(PackageVersion).nupkg",
        
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
        "Metalama.Framework.Tests.UnitTestHelpers.$(PackageVersion).nupkg" ),
    ParametrizedDependencies =
    [
        DevelopmentDependencies.PostSharpEngineering.ToDependency(),
        MetalamaDependencies.MetalamaCompiler.ToDependency(
            new ConfigurationSpecific<BuildConfiguration>(
                BuildConfiguration.Release, BuildConfiguration.Release, BuildConfiguration.Public
            ) )
    ],
    ExportedProperties =
    {
        { "Metalama.Framework\\Directory.Packages.props", ["RoslynApiMaxVersion", "RoslynMaxVersion"] }, { "Metalama.Framework\\Directory.Build.props", ["LangMaxVersion"] }
    },
    Configurations = Product.DefaultConfigurations
        .WithValue(
            BuildConfiguration.Debug,
            c => c with
            {
                AdditionalArtifactRules =
                [
                    $@"+:%system.teamcity.build.tempDir%/Metalama/ExtractExceptions/**/*=>logs",
                    $@"+:%system.teamcity.build.tempDir%/Metalama/Extract/**/.completed=>logs",
                    $@"+:%system.teamcity.build.tempDir%/Metalama/CrashReports/**/*=>logs",

                    // Do not upload uncompressed crash reports because they are too big.
                    $@"-:%system.teamcity.build.tempDir%/Metalama/CrashReports/**/*.dmp=>logs"
                ]
            } ),
    SupportedProperties =
    {
        { "PrepareStubs", "The prepare command generates stub files, instead of actual implementations." }
    },
};

product.PrepareCompleted += OnPrepareCompleted;

return new EngineeringApp( product ).Run( args );

static void OnPrepareCompleted( PrepareCompletedEventArgs arg )
{
    TestLicenseKeyDownloader.Download( arg.Context );

    arg.Context.Console.WriteHeading( "Generating code" );

    var srcDirectory = Path.Combine( arg.Context.RepoDirectory, "Metalama.Framework" );

    GenerateMetaSyntaxRewriter.Generate( srcDirectory );
}