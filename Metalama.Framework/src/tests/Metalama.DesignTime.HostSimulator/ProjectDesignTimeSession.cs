// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.DesignTime.HostSimulator;

/// <summary>
/// Runs, for a single project, the work an IDE performs at design time: source generation followed by the analyzer
/// requests an open document triggers.
/// </summary>
/// <remarks>
/// The sequence follows what Roslyn does in an editor. Source generators run first and their output becomes part of
/// the compilation, because analyzers must see generated code. Analyzers then run per document rather than per
/// compilation, because an editor asks for the diagnostics of the documents it is showing, which is what makes the
/// order of requests observable to a stateful analyzer such as Metalama.
/// </remarks>
internal sealed class ProjectDesignTimeSession
{
    private readonly Project _project;
    private readonly IsolatedAnalyzerAssemblyLoader _loader;
    private readonly SimulationReport _report;

    public ProjectDesignTimeSession( Project project, IsolatedAnalyzerAssemblyLoader loader, SimulationReport report )
    {
        this._project = project;
        this._loader = loader;
        this._report = report;
    }

    public async Task RunAsync( CancellationToken cancellationToken )
    {
        var projectReport = this._report.GetOrAddProject( this._project.Name );

        var compilation = await this._project.GetCompilationAsync( cancellationToken );

        if ( compilation == null )
        {
            projectReport.AddError( $"The project '{this._project.Name}' produced no compilation." );

            return;
        }

        // Re-create the analyzer references with our own loader, so that analyzers are isolated per directory the
        // way Roslyn isolates them, rather than sharing the loader the workspace happens to provide.
        var references = this._project.AnalyzerReferences
            .OfType<AnalyzerFileReference>()
            .Select( reference => new AnalyzerFileReference( reference.FullPath, this._loader ) )
            .ToImmutableArray();

        foreach ( var reference in references )
        {
            this._loader.AddDependencyLocation( reference.FullPath );

            reference.AnalyzerLoadFailed += ( _, e ) =>
                projectReport.AddError( $"Cannot load analyzers from '{reference.FullPath}': {e.ErrorCode}: {e.Message}" );
        }

        var analyzers = references.SelectMany( r => r.GetAnalyzers( LanguageNames.CSharp ) ).ToImmutableArray();
        var generators = references.SelectMany( r => r.GetGenerators( LanguageNames.CSharp ) ).ToImmutableArray();

        projectReport.AnalyzerCount = analyzers.Length;
        projectReport.GeneratorCount = generators.Length;

        var compilationWithGeneratedCode = this.RunGenerators( compilation, generators, projectReport, cancellationToken );

        if ( analyzers.IsEmpty )
        {
            return;
        }

        await this.RunAnalyzersAsync( compilationWithGeneratedCode, analyzers, projectReport, cancellationToken );
    }

    /// <summary>
    /// Runs the source generators and returns the compilation that includes their output.
    /// </summary>
    private Compilation RunGenerators(
        Compilation compilation,
        ImmutableArray<ISourceGenerator> generators,
        ProjectReport projectReport,
        CancellationToken cancellationToken )
    {
        if ( generators.IsEmpty )
        {
            return compilation;
        }

        try
        {
            var driver = CSharpGeneratorDriver.Create(
                generators,
                this._project.AnalyzerOptions.AdditionalFiles,
                (CSharpParseOptions?) this._project.ParseOptions,
                this._project.AnalyzerOptions.AnalyzerConfigOptionsProvider );

            driver.RunGeneratorsAndUpdateCompilation( compilation, out var outputCompilation, out var diagnostics, cancellationToken );

            projectReport.AddDiagnostics( diagnostics );

            projectReport.GeneratedDocumentCount =
                outputCompilation.SyntaxTrees.Count() - compilation.SyntaxTrees.Count();

            return outputCompilation;
        }
        catch ( Exception exception )
        {
            projectReport.AddError( $"Source generation threw {exception.GetType().Name}: {exception.Message}" );

            return compilation;
        }
    }

    /// <summary>
    /// Issues, for every document of the project, the syntax and semantic analyzer requests an editor issues when
    /// the document is open.
    /// </summary>
    private async Task RunAnalyzersAsync(
        Compilation compilation,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        ProjectReport projectReport,
        CancellationToken cancellationToken )
    {
        var analysisOptions = new CompilationWithAnalyzersOptions(
            this._project.AnalyzerOptions,
            onAnalyzerException: ( exception, analyzer, _ ) =>
                projectReport.AddError( $"The analyzer '{analyzer.GetType().FullName}' threw {exception.GetType().Name}: {exception.Message}" ),
            concurrentAnalysis: false,
            logAnalyzerExecutionTime: false );

        var compilationWithAnalyzers = compilation.WithAnalyzers( analyzers, analysisOptions );

        foreach ( var syntaxTree in compilation.SyntaxTrees )
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                projectReport.AddDiagnostics( await compilationWithAnalyzers.GetAnalyzerSyntaxDiagnosticsAsync( syntaxTree, cancellationToken ) );

                var semanticModel = compilation.GetSemanticModel( syntaxTree );

                projectReport.AddDiagnostics(
                    await compilationWithAnalyzers.GetAnalyzerSemanticDiagnosticsAsync( semanticModel, null, cancellationToken ) );
            }
            catch ( Exception exception ) when ( exception is not OperationCanceledException )
            {
                projectReport.AddError( $"Analyzing '{syntaxTree.FilePath}' threw {exception.GetType().Name}: {exception.Message}" );
            }
        }
    }
}
