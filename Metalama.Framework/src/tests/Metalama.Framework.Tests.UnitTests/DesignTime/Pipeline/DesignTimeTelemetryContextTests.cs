// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime;
using Metalama.Framework.DesignTime.DiagnosticAnalysis;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using SharpCrafters.Backstage.Extensibility;
using SharpCrafters.Backstage.Telemetry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Tests that the design-time pipeline opens one telemetry context per project, that the exceptions of the project are
/// reported to this context, and that the warnings of the context are reported once per project. See #2421.
/// </summary>
public sealed class DesignTimeTelemetryContextTests : UnitTestClass
{
    public DesignTimeTelemetryContextTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    /// <summary>
    /// Checks that the pipeline opens a telemetry context when it is created and exposes it untyped in the service provider of
    /// the project.
    /// </summary>
    [Fact]
    public void PipelineRegistersTelemetryContextInProjectServiceProvider()
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var compilation = testContext.CreateCSharpCompilation( "class C {}" );
        var pipeline = factory.GetOrCreatePipeline( testContext.ProjectOptions, compilation );

        Assert.NotNull( pipeline );
        Assert.NotNull( pipeline.TelemetryContext );
        Assert.Same( pipeline.TelemetryContext, pipeline.ServiceProvider.Underlying.GetService( typeof(ITelemetryContext) ) );
    }

    /// <summary>
    /// Checks that an exception of a project is reported to the context of its pipeline, and not to a context opened for the
    /// project options given to the handler.
    /// </summary>
    [Fact]
    public void ExceptionOfProjectIsReportedToPipelineContext()
    {
        using var testContext = this.CreateTestContext();
        testContext.ExpectsReportedExceptions = true;
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var compilation = testContext.CreateCSharpCompilation( "class C {}" );
        var pipeline = factory.GetOrCreatePipeline( testContext.ProjectOptions, compilation );
        Assert.NotNull( pipeline );

        var handler = factory.ServiceProvider.GetService<DesignTimeExceptionHandler>();
        Assert.NotNull( handler );
        var otherProjectPath = Path.Combine( Path.GetTempPath(), "other-repo", "Other.csproj" );

        handler.ReportException( new InvalidOperationException(), pipeline.ProjectKey, new ProjectOptionsStub( otherProjectPath ) );

        var report = Assert.Single( testContext.ReportedTelemetryExceptions );
        Assert.NotEqual( Path.GetDirectoryName( otherProjectPath ), report.Directory );
        Assert.Equal( GetExpectedDirectory( testContext.ProjectOptions ), report.Directory );
    }

    /// <summary>
    /// Checks that the handler opens a context for the project directory when no pipeline registered a context for the
    /// project, and that the registration of a disposed pipeline is removed.
    /// </summary>
    [Fact]
    public void ExceptionOfProjectWithoutRegisteredContextOpensContext()
    {
        using var testContext = this.CreateTestContext();
        testContext.ExpectsReportedExceptions = true;

        var handler = testContext.ServiceProvider.Global.GetRequiredService<DesignTimeExceptionHandler>();
        var projectKey = ProjectKeyFactory.CreateTest( "Test" );
        var projectPath = Path.Combine( Path.GetTempPath(), "repo", "Project.csproj" );
        var registeredDirectory = Path.Combine( Path.GetTempPath(), "registered" );

        var telemetryService = testContext.ServiceProvider.Global.Underlying.GetBackstageService<ITelemetryService>();
        Assert.NotNull( telemetryService );
        var registeredContext = telemetryService.OpenContext( telemetryService.GetPolicy( registeredDirectory ) );

        handler.RegisterProjectTelemetryContext( projectKey, registeredContext );
        handler.ReportException( new InvalidOperationException(), projectKey, new ProjectOptionsStub( projectPath ) );

        handler.UnregisterProjectTelemetryContext( projectKey, registeredContext );
        handler.ReportException( new InvalidOperationException(), projectKey, new ProjectOptionsStub( projectPath ) );

        Assert.Equal(
            new[] { Path.GetDirectoryName( projectPath ), registeredDirectory }.OrderBy( d => d ),
            testContext.ReportedTelemetryExceptions.SelectAsReadOnlyCollection( r => r.Directory ).OrderBy( d => d ) );
    }

    /// <summary>
    /// Checks that a warning of the telemetry context, such as an invalid <c>metalama.json</c> file, is reported as LAMA0075
    /// on the first syntax tree of the compilation only.
    /// </summary>
    [Fact]
    public void TelemetryContextWarningIsReportedOncePerProject()
    {
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddGlobalService<IUserDiagnosticRegistrationService>( new TestUserDiagnosticRegistrationService() );

        using var testContext = this.CreateTestContext( additionalServices );

        var metalamaJsonPath = Path.Combine( Path.GetTempPath(), "repo", "metalama.json" );
        testContext.TelemetryContextWarnings = ImmutableArray.Create( new TelemetryContextWarning( "The file is malformed.", metalamaJsonPath ) );

        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["First.cs"] = "class First {}", ["Second.cs"] = "class Second {}" } );

        var analyzer = new TheDiagnosticAnalyzer( factory.ServiceProvider );

        var diagnosticsByTree = compilation.SyntaxTrees
            .Select(
                tree =>
                {
                    var analysisContext = new TestSemanticModelAnalysisContext( compilation.GetSemanticModel( tree ), testContext.ProjectOptions );
                    analyzer.AnalyzeSemanticModel( analysisContext );

                    return analysisContext.ReportedDiagnostics.Where( d => d.Id == GeneralDiagnosticDescriptors.InvalidRepositoryConfiguration.Id ).ToList();
                } )
            .ToList();

        var diagnostic = Assert.Single( diagnosticsByTree[0] );
        Assert.Contains( metalamaJsonPath, diagnostic.GetMessage(), StringComparison.Ordinal );
        Assert.All( diagnosticsByTree.Skip( 1 ), Assert.Empty );
    }

    /// <summary>
    /// Checks that the warning is reported once when the first syntax tree of the compilation is generated code. Roslyn does
    /// not run the analyzer on a generated tree, so the warning must be reported on the first tree that is not generated.
    /// </summary>
    [Fact]
    public void TelemetryContextWarningIsReportedWhenFirstTreeIsGenerated()
    {
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddGlobalService<IUserDiagnosticRegistrationService>( new TestUserDiagnosticRegistrationService() );

        using var testContext = this.CreateTestContext( additionalServices );

        var metalamaJsonPath = Path.Combine( Path.GetTempPath(), "repo", "metalama.json" );
        testContext.TelemetryContextWarnings = ImmutableArray.Create( new TelemetryContextWarning( "The file is malformed.", metalamaJsonPath ) );

        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string>
            {
                ["AssemblyAttributes.cs"] = "// <auto-generated/>\n[assembly: System.Reflection.AssemblyTitle( \"Test\" )]",
                ["First.cs"] = "class First {}",
                ["Second.cs"] = "class Second {}"
            } );

        var generatedTree = compilation.SyntaxTrees.First();
        Assert.Equal( "AssemblyAttributes.cs", Path.GetFileName( generatedTree.FilePath ) );

        var analyzer = new TheDiagnosticAnalyzer( factory.ServiceProvider );

        // Analyze the trees as Roslyn does: the generated tree is not analyzed.
        var diagnostics = compilation.SyntaxTrees
            .Where( tree => tree != generatedTree )
            .SelectMany(
                tree =>
                {
                    var analysisContext = new TestSemanticModelAnalysisContext( compilation.GetSemanticModel( tree ), testContext.ProjectOptions );
                    analyzer.AnalyzeSemanticModel( analysisContext );

                    return analysisContext.ReportedDiagnostics.Where( d => d.Id == GeneralDiagnosticDescriptors.InvalidRepositoryConfiguration.Id );
                } )
            .ToList();

        var diagnostic = Assert.Single( diagnostics );
        Assert.Equal( "First.cs", Path.GetFileName( diagnostic.Location.SourceTree?.FilePath ) );
    }

    private static string? GetExpectedDirectory( IProjectOptions projectOptions )
        => string.IsNullOrEmpty( projectOptions.ProjectPath ) ? null : Path.GetDirectoryName( projectOptions.ProjectPath );

    /// <summary>
    /// Project options with a given project path.
    /// </summary>
    private sealed class ProjectOptionsStub : DefaultProjectOptions
    {
        public ProjectOptionsStub( string? projectPath )
        {
            this.ProjectPath = projectPath;
        }

        public override string? ProjectPath { get; }
    }
}
