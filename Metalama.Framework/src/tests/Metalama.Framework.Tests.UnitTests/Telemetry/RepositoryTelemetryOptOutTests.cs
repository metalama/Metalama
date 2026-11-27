// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler.Services;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Testing;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTests.CompileTime;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SharpCrafters.Backstage.Telemetry;
using SharpCrafters.Backstage.Testing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Telemetry;

/// <summary>
/// Tests that a project whose repository disables telemetry in <c>metalama.json</c> sends no usage session and no exception
/// report, in the compiler and at design time, and that the local crash report is still written. See #2420 and #2421.
/// </summary>
/// <remarks>
/// <para>
/// The tests run the real <see cref="ITelemetryService"/> of Backstage through <see cref="TestTelemetryEnvironment"/>, so the
/// policy is resolved from a <c>metalama.json</c> file of an in-memory repository as in production. The environment records
/// what the telemetry contexts allow, instead of sending it.
/// </para>
/// <para>
/// An exception has two outputs. The report for upload is created only when the policy allows telemetry, and
/// <see cref="TestTelemetryEnvironment.ExceptionCapturer"/> records it. The local crash report is written regardless of the
/// policy, and <see cref="TestTelemetryEnvironment.GetLocalCrashReports"/> lists it.
/// </para>
/// <para>
/// Each test runs twice: once with the opt-out, and once with a repository that does not disable telemetry. The second run
/// proves that the code path reaches the telemetry context, so that the empty result of the first run is not vacuous.
/// </para>
/// </remarks>
public sealed class RepositoryTelemetryOptOutTests : UnitTestClass
{
    private const string _assemblyName = "Project";
    private const string _code = "class C {}";

    private static readonly string _repositoryRoot = Path.Combine( Path.GetTempPath(), "metalama-2420-repository" );
    private static readonly string _projectPath = Path.Combine( _repositoryRoot, "src", "Project", "Project.csproj" );

    public RepositoryTelemetryOptOutTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    /// <summary>
    /// Checks that the compiler starts no usage session for a project whose repository disables telemetry.
    /// </summary>
    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void CompilerUsageSession( bool isDisabledByRepository )
    {
        var environment = CreateEnvironment( isDisabledByRepository );
        using var testContext = this.CreateTestContext( CreateServices( environment ) );

        var compilerServices = CreateCompilerServiceProvider( testContext );
        compilerServices.DisposeServices( _ => { } );

        if ( isDisabledByRepository )
        {
            Assert.Empty( environment.UsageSessionFactory.Sessions );
        }
        else
        {
            var session = Assert.Single( environment.UsageSessionFactory.Sessions );
            Assert.Equal( "TransformerUsage", session.Kind );
        }
    }

    /// <summary>
    /// Checks that an exception that Metalama.Compiler reports through the <see cref="IExceptionReporter"/> of the project is not
    /// captured when the repository disables telemetry, and that its local crash report is written in both cases.
    /// </summary>
    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void CompilerExceptionReporter( bool isDisabledByRepository )
    {
        var environment = CreateEnvironment( isDisabledByRepository );
        using var testContext = this.CreateTestContext( CreateServices( environment ) );

        var compilerServices = CreateCompilerServiceProvider( testContext );
        var exceptionReporter = (IExceptionReporter?) compilerServices.GetService( typeof(IExceptionReporter) );
        Assert.NotNull( exceptionReporter );

        exceptionReporter.ReportException( new InvalidOperationException() );
        compilerServices.DisposeServices( _ => { } );

        Assert.Equal( isDisabledByRepository ? 0 : 1, environment.ExceptionCapturer.CapturedReports.Count );
        Assert.Single( environment.GetLocalCrashReports() );
    }

    /// <summary>
    /// Checks that an exception of the compile-time pipeline, which <see cref="CompileTimeExceptionHandler"/> reports to the
    /// telemetry context of the compiler, is not captured when the repository disables telemetry.
    /// </summary>
    /// <remarks>
    /// The handler writes its own crash report and cites it in the diagnostic, so it asks the telemetry context not to write a
    /// second one. No local crash report of Backstage is therefore expected, whatever the policy is.
    /// </remarks>
    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void CompileTimePipelineException( bool isDisabledByRepository )
    {
        var environment = CreateEnvironment( isDisabledByRepository );
        using var testContext = this.CreateTestContext( CreateServices( environment ) );
        testContext.FaultInjector.ArmFault( FaultInjectionPoints.CompileTimePipeline );

        var compilerServices = CreateCompilerServiceProvider( testContext );
        var telemetryContext = (ITelemetryContext?) compilerServices.GetService( typeof(ITelemetryContext) );
        Assert.NotNull( telemetryContext );

        var transformerContext = new TestTransformerContext(
            testContext.CreateCSharpCompilation( _code ),
            new ProjectOptionsStub( _projectPath ),
            testContext.ServiceProvider.Global,
            telemetryContext );

        SourceTransformer.Execute( transformerContext );
        compilerServices.DisposeServices( _ => { } );

        Assert.Equal( isDisabledByRepository ? 0 : 1, environment.ExceptionCapturer.CapturedReports.Count );
        Assert.Empty( environment.GetLocalCrashReports() );
    }

    /// <summary>
    /// Checks that an exception that <see cref="DesignTimeExceptionHandler"/> reports for a project is not captured when the
    /// repository of the project disables telemetry, and that its local crash report is written in both cases.
    /// </summary>
    [Theory]
    [InlineData( true )]
    [InlineData( false )]
    public void DesignTimeException( bool isDisabledByRepository )
    {
        var environment = CreateEnvironment( isDisabledByRepository );
        using var testContext = this.CreateTestContext( CreateServices( environment ) );
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var projectOptions = new ProjectOptionsStub( _projectPath );
        var pipeline = factory.GetOrCreatePipeline( projectOptions, testContext.CreateCSharpCompilation( _code ) );
        Assert.NotNull( pipeline );

        var handler = factory.ServiceProvider.GetService<DesignTimeExceptionHandler>();
        Assert.NotNull( handler );

        handler.ReportException( new InvalidOperationException(), pipeline.ProjectKey, projectOptions );

        Assert.Equal( isDisabledByRepository ? 0 : 1, environment.ExceptionCapturer.CapturedReports.Count );
        Assert.Single( environment.GetLocalCrashReports() );
    }

    /// <summary>
    /// Creates a telemetry environment with a repository that contains the project. When <paramref name="isDisabledByRepository"/>
    /// is <see langword="true"/>, the <c>metalama.json</c> file of the repository disables telemetry. Otherwise, the repository
    /// has no <c>metalama.json</c> file.
    /// </summary>
    private static TestTelemetryEnvironment CreateEnvironment( bool isDisabledByRepository )
    {
        var environment = new TestTelemetryEnvironment();
        environment.CreateRepository( _repositoryRoot, isDisabledByRepository ? false : null );

        return environment;
    }

    /// <summary>
    /// Creates the additional services that replace the telemetry service of the test context with the real service of
    /// <paramref name="environment"/>.
    /// </summary>
    private static AdditionalServiceCollection CreateServices( TestTelemetryEnvironment environment )
    {
        var services = new AdditionalServiceCollection();
        services.BackstageServices.Add( environment.TelemetryService, allowOverride: true );

        return services;
    }

    /// <summary>
    /// Creates the service provider that <see cref="SourceTransformer.InitializeServices"/> returns to Metalama.Compiler for the
    /// project.
    /// </summary>
    private static SourceTransformer.CompilerServiceProvider CreateCompilerServiceProvider( MetalamaTestContext testContext )
        => new( testContext.ServiceProvider.Global.Underlying, new ProjectOptionsProvider( _projectPath, _assemblyName ) );

    /// <summary>
    /// Project options with a given project path.
    /// </summary>
    private sealed class ProjectOptionsStub : DefaultProjectOptions
    {
        public ProjectOptionsStub( string projectPath )
        {
            this.ProjectPath = projectPath;
        }

        public override string? ProjectPath { get; }

        public override string? AssemblyName => RepositoryTelemetryOptOutTests._assemblyName;
    }

    /// <summary>
    /// An <see cref="AnalyzerConfigOptionsProvider"/> whose global options give the MSBuild properties that the compiler reads.
    /// </summary>
    private sealed class ProjectOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public ProjectOptionsProvider( string projectPath, string assemblyName )
        {
            this.GlobalOptions = new Options(
                new Dictionary<string, string>
                {
                    [$"build_property.{MSBuildPropertyNames.MSBuildProjectFullPath}"] = projectPath,
                    [$"build_property.{MSBuildPropertyNames.AssemblyName}"] = assemblyName
                } );
        }

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions( SyntaxTree tree ) => Options.Empty;

        public override AnalyzerConfigOptions GetOptions( AdditionalText textFile ) => Options.Empty;

        /// <summary>
        /// Analyzer configuration options backed by a dictionary.
        /// </summary>
        private sealed class Options : AnalyzerConfigOptions
        {
            public static readonly Options Empty = new( new Dictionary<string, string>() );

            private readonly Dictionary<string, string> _values;

            public Options( Dictionary<string, string> values )
            {
                this._values = values;
            }

            public override bool TryGetValue( string key, [NotNullWhen( true )] out string? value ) => this._values.TryGetValue( key, out value );
        }
    }
}
