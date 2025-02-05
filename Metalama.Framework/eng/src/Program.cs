// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.GenerateMetaSyntaxRewriter;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Build.Solutions;
using PostSharp.Engineering.BuildTools.Dependencies.Definitions;
using PostSharp.Engineering.BuildTools.Dependencies.Model;
using PostSharp.Engineering.BuildTools.Utilities;
using System.IO;
using System.Runtime.InteropServices;
using MetalamaDependencies = PostSharp.Engineering.BuildTools.Dependencies.Definitions.MetalamaDependencies.V2025_1;

var product = new Product( MetalamaDependencies.Metalama )
{
    Solutions =
    [
        new DotNetSolution( "Metalama.sln" )
        {
            SolutionFilterPathForInspectCode = "Metalama.LatestRoslyn.slnf",
            SupportsTestCoverage = true,
            CanFormatCode = true,

            // We don't run the tests for the whole solution because they are too slow and redundant. See #34277.
            TestMethod = BuildMethod.None,
            FormatExclusions =
            [
                // Test payloads should not be formatted because it would break the test output comparison.
                // In some cases, formatting or redundant keywords may be intentional.
                "Tests\\Metalama.Framework.Tests.AspectTests\\Tests\\**\\*",
                "Tests\\Metalama.Framework.Tests.LinkerTests\\Tests\\**\\*",
                "Tests\\Metalama.Framework.Tests.TemplateTests\\Tests\\**\\*",
                "Tests\\Metalama.Extensions.*.AspectTests\\**\\*",
                "**\\*.g.cs",

                // XML formatting seems to be conflicting.
                "**\\*.props", "**\\*.targets", "**\\*.csproj", "**\\*.md", "**\\*.xml", "**\\*.config"
            ]
        },
        new DotNetSolution( "Metalama.LatestRoslyn.slnf" )
        {
            SupportsTestCoverage = false, CanFormatCode = false, IsTestOnly = true
        },
        new DotNetSolution( "Tests\\Metalama.Framework.TestApp\\Metalama.Framework.TestApp.sln" )
        {
            IsTestOnly = true, TestMethod = BuildMethod.Build
        },
        new ManyDotNetSolutions( "Tests\\Standalone" ) { IsTestOnly = true }
    ],
    PublicArtifacts = Pattern.Create(
        "Metalama.Framework.$(PackageVersion).nupkg",
        "Metalama.Testing.UnitTesting.$(PackageVersion).nupkg",
        "Metalama.Testing.AspectTesting.$(PackageVersion).nupkg",
        "Metalama.Framework.Redist.$(PackageVersion).nupkg",
        "Metalama.Framework.Sdk.$(PackageVersion).nupkg",
        "Metalama.Framework.Engine.$(PackageVersion).nupkg",
        "Metalama.Framework.CompileTimeContracts.$(PackageVersion).nupkg",
        "Metalama.Framework.Introspection.$(PackageVersion).nupkg",
        "Metalama.Framework.Workspaces.$(PackageVersion).nupkg",
        "Metalama.Tool.$(PackageVersion).nupkg" ),
    PrivateArtifacts = Pattern.Create(
        "Metalama.Framework.Tests.UnitTestHelpers.$(PackageVersion).nupkg" ),
    ParametrizedDependencies =
    [
        DevelopmentDependencies.PostSharpEngineering.ToDependency(),
        MetalamaDependencies.MetalamaBackstage.ToDependency(),
        MetalamaDependencies.MetalamaCompiler.ToDependency(
            new ConfigurationSpecific<BuildConfiguration>(
                BuildConfiguration.Release, BuildConfiguration.Release, BuildConfiguration.Public
            ) )
    ],
    ExportedProperties =
    {
        { "Directory.Packages.props", ["RoslynApiMaxVersion", "RoslynMaxVersion"] }, { "Directory.Build.props", ["LangMaxVersion"] }
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
product.PrepareCompleted += args => TestLicenseKeyDownloader.Download( args.Context );

return new EngineeringApp( product ).Run( args );

static void OnPrepareCompleted( PrepareCompletedEventArgs arg )
{
    TestLicenseKeyDownloader.Download( arg.Context );

    arg.Context.Console.WriteHeading( "Generating code" );

    var srcDirectory = arg.Context.RepoDirectory;

    GenerateMetaSyntaxRewriter.Generate( srcDirectory );
}