// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.DiagnosticAnalysis;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.EndToEnd;

/// <summary>
/// Tests that the design-time results of a project-local kind (<see cref="ContributorKind.IsProjectTransitive"/> is <c>false</c>) stay in the project that produced
/// them, when another project references that project.
/// </summary>
/// <remarks>
/// The extension of these tests returns a project-local design-time result for each new contributor. The result records the name of the
/// assembly that produced it, and throws when its transitive form is requested. The test harness fails when the pipeline reports an exception,
/// so the export of a project-local result to the referencing project fails the test.
/// </remarks>
public sealed class ProjectLocalExtensionTests : UnitTestClass
{
    /// <summary>
    /// The code of both projects: a project fabric that adds a diagnostic query, which is the contributor for which the extension returns a
    /// project-local result.
    /// </summary>
    private const string _code = """
                                 using Metalama.Framework.Aspects;
                                 using Metalama.Framework.Code;
                                 using Metalama.Framework.Diagnostics;
                                 using Metalama.Framework.Fabrics;

                                 [CompileTime]
                                 internal static class Definitions
                                 {
                                     public static readonly DiagnosticDefinition Warning = new( "MY001", Severity.Hidden, "Hidden." );
                                 }

                                 internal class Fabric : ProjectFabric
                                 {
                                     public override void AmendProject( IProjectAmender amender )
                                         => amender.SelectTypes().ReportDiagnostic( _ => Definitions.Warning );
                                 }

                                 public class C { }
                                 """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectLocalExtensionTests"/> class.
    /// </summary>
    public ProjectLocalExtensionTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    /// <summary>
    /// Verifies that the analysis of each project receives the project-local results of that project only, and not those of the project that it
    /// references.
    /// </summary>
    [Fact]
    public async Task ReferencingProject_DoesNotReceiveProjectLocalExtensions()
    {
        var recorder = new Recorder();
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( recorder );
        additionalServices.AddGlobalService<IUserDiagnosticRegistrationService>( new TestUserDiagnosticRegistrationService() );

        using var testContext = this.CreateTestContext(
            this.CreateDefaultTestContextOptions() with
            {
                ExtensionTypes = ImmutableArray.Create( typeof(TestExtension) ), DesignTimeExtensionTypes = ImmutableArray.Create( typeof(TestExtension) )
            },
            additionalServices );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );
        var workspaceProvider = new TestWorkspaceProvider( testContext.ServiceProvider );

        workspaceProvider.AddOrUpdateProject( testContext, "dependency", new Dictionary<string, string> { ["dependency.cs"] = _code } );

        workspaceProvider.AddOrUpdateProject(
            testContext,
            "consumer",
            new Dictionary<string, string> { ["consumer.cs"] = _code.Replace( "class C", "class D" ) },
            projectReferences: ["dependency"] );

        var analyzer = new TheDiagnosticAnalyzer( pipelineFactory.ServiceProvider );

        foreach ( var (project, document) in new[] { ("dependency", "dependency.cs"), ("consumer", "consumer.cs") } )
        {
            var compilation = await workspaceProvider.GetProject( project ).GetCompilationAsync( testContext.CancellationToken );
            var syntaxTree = await workspaceProvider.GetDocument( project, document ).GetSyntaxTreeAsync( testContext.CancellationToken );

            analyzer.AnalyzeSemanticModel( new TestSemanticModelAnalysisContext( compilation!.GetSemanticModel( syntaxTree! ), testContext.ProjectOptions ) );
        }

        // The analysis of each project sees the project-local results of that project only.
        Assert.Equal( ["consumer", "dependency"], recorder.AnalyzedProjectLocalResults.Distinct().OrderBy( x => x, StringComparer.Ordinal ) );
        Assert.Equal( ["dependency"], recorder.AnalyzedProjectLocalResultsByAnalyzedProject["dependency"].Distinct() );
        Assert.Equal( ["consumer"], recorder.AnalyzedProjectLocalResultsByAnalyzedProject["consumer"].Distinct() );
    }

    /// <summary>
    /// Records what <see cref="TestExtension"/> observes.
    /// </summary>
    private sealed class Recorder : IProjectService
    {
        /// <summary>
        /// Gets the assembly names of the project-local results that <see cref="PipelineExtension.AnalyzeSemanticModel"/> received, for all projects.
        /// </summary>
        public ConcurrentQueue<string> AnalyzedProjectLocalResults { get; } = new();

        /// <summary>
        /// Gets the assembly names of the project-local results that <see cref="PipelineExtension.AnalyzeSemanticModel"/> received, indexed by the
        /// assembly name of the analyzed project.
        /// </summary>
        public ConcurrentDictionary<string, ConcurrentQueue<string>> AnalyzedProjectLocalResultsByAnalyzedProject { get; } = new();
    }

    private sealed class TestExtension : PipelineExtension
    {
        /// <summary>
        /// The recorder of the project, or <c>null</c> when the test did not register one.
        /// </summary>
        private Recorder? _recorder;

        public override bool Initialize( PipelineExtensionInitializationContext context )
        {
            this._recorder = context.ServiceProvider.GetService<Recorder>();

            return true;
        }

        public override Task<ExtensionPipelineContributorsResult> ExecuteDesignTimePipelineContributorsAsync(
            DesignTimeContributorsContext context,
            CancellationToken cancellationToken )
        {
            var assemblyName = context.StageInitialCompilation.RoslynCompilation.AssemblyName!;

            var results = ImmutableArray.CreateRange( context.Contributors.SelectAsArray( ITransitivePipelineContributor ( _ ) => new ProjectLocalResult( assemblyName ) ) );

            return Task.FromResult( new ExtensionPipelineContributorsResult( results, ImmutableUserDiagnosticList.Empty ) );
        }

        public override ImmutableUserDiagnosticList AnalyzeSemanticModel(
            AspectPipelineConfiguration pipelineConfiguration,
            SemanticModel semanticModel,
            DesignTimeAspectPipelineResultExtensionCollection extensions,
            AspectRepository aspectRepository,
            CancellationToken cancellationToken )
        {
            var analyzedProject = semanticModel.Compilation.AssemblyName!;

            foreach ( var result in extensions.Extensions.OfType<ProjectLocalResult>() )
            {
                this._recorder?.AnalyzedProjectLocalResults.Enqueue( result.AssemblyName );
                this._recorder?.AnalyzedProjectLocalResultsByAnalyzedProject.GetOrAdd( analyzedProject, _ => new ConcurrentQueue<string>() ).Enqueue( result.AssemblyName );
            }

            return ImmutableUserDiagnosticList.Empty;
        }
    }

    /// <summary>
    /// A project-local design-time result.
    /// </summary>
    private sealed class ProjectLocalResult : ITransitivePipelineContributor, IDesignTimePipelineResultExtension
    {
        /// <summary>
        /// The project-local kind of <see cref="ProjectLocalResult"/>.
        /// </summary>
        private static readonly ContributorKind<ProjectLocalResult> _kind = new( nameof(ProjectLocalResult) ) { IsProjectTransitive = false };

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectLocalResult"/> class.
        /// </summary>
        public ProjectLocalResult( string assemblyName )
        {
            this.AssemblyName = assemblyName;
        }

        /// <summary>
        /// Gets the name of the assembly of the project that produced the result.
        /// </summary>
        public string AssemblyName { get; }

        /// <inheritdoc />
        public ContributorKind ContributorKind => _kind;

        /// <inheritdoc />
        public DocumentKey DocumentKey => default;

        /// <inheritdoc />
        public IDesignTimePipelineResultExtension ToDesignTime() => this;

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/>, because the transitive form of a project-local result must never be requested.
        /// </summary>
        public ITransitiveAspectsManifestExtension ToTransitiveAspectManifestExtension()
            => throw new InvalidOperationException( "The transitive form of a project-local result must never be requested." );
    }
}
