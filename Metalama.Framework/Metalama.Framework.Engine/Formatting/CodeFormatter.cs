// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Formatting;

public sealed partial class CodeFormatter : IProjectService
{
    private readonly IConcurrentTaskRunner _concurrentTaskRunner;

    internal CodeFormatter( ProjectServiceProvider serviceProvider )
    {
        this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
    }

    // Use this constructor when no ProjectServiceProvider is available.
    // This happens in tests and at design time for actions that are not project-scoped.
    public CodeFormatter()
    {
        this._concurrentTaskRunner = new ConcurrentTaskRunner();
    }

    private async Task<Solution> TransformAllAsync(
        Solution solution,
        IEnumerable<DocumentId> documentIds,
        Func<Document, Task<Document>> transformAsync,
        CancellationToken cancellationToken )
    {
        var modifiedDocuments = new ConcurrentQueue<Document>();

        await this._concurrentTaskRunner.RunConcurrentlyAsync( documentIds, ModifyDocumentAsync, cancellationToken );

        async Task ModifyDocumentAsync( DocumentId documentId )
        {
            var oldDocument = solution.GetDocument( documentId );

            if ( oldDocument == null )
            {
                return;
            }

            var newDocument = await transformAsync( oldDocument );

            if ( newDocument != oldDocument )
            {
                modifiedDocuments.Enqueue( newDocument );
            }
        }

        var modifiedSolution = solution;

        foreach ( var modifiedDocument in modifiedDocuments )
        {
            var modifiedSyntaxRoot = await modifiedDocument.GetSyntaxRootAsync( cancellationToken );

            modifiedSolution = modifiedSolution.GetDocument( modifiedDocument.Id )
                .AssertNotNull()
                .WithSyntaxRoot( modifiedSyntaxRoot.AssertNotNull() )
                .Project.Solution;
        }

        return modifiedSolution;
    }

    public async ValueTask<Document> FormatAsync(
        Document document,
        IReadOnlyCollection<Diagnostic>? diagnostics = null,
        bool reformatAll = true,
        CancellationToken cancellationToken = default )
    {
        var modifiedSolution = await this.FormatAsync( document.Project.Solution, [document.Id], diagnostics, reformatAll, cancellationToken );

        return modifiedSolution.GetDocument( document.Id ).AssertNotNull();
    }

    internal async ValueTask<Solution> FormatAsync(
        Solution solution,
        IReadOnlyCollection<DocumentId> documentIds,
        IReadOnlyCollection<Diagnostic>? diagnostics = null,
        bool reformatAll = true,
        CancellationToken cancellationToken = default )
    {
        if ( documentIds.Count == 0 )
        {
            return solution;
        }

        var modifiedSolution = solution;

        // Add diagnostics as annotations.
        if ( diagnostics is { Count: > 0 } )
        {
            modifiedSolution = await this.TransformAllAsync(
                modifiedSolution,
                documentIds,
                async document => document.WithSyntaxRoot(
                    FormattedCodeWriter.AddDiagnosticAnnotations(
                        (await document.GetSyntaxRootAsync( cancellationToken ))!,
                        document.FilePath,
                        diagnostics ) ),
                cancellationToken );
        }

        // Run custom simplifications.
        modifiedSolution = await this.TransformAllAsync(
            modifiedSolution,
            documentIds,
            async document =>
            {
                var oldRoot = await document.GetSyntaxRootAsync( cancellationToken );

                // Try a first time without a semantic mode. 
                var rewriterWithoutSemanticModel = new CustomSimplifier( null );
                var newRoot = rewriterWithoutSemanticModel.Visit( oldRoot )!;

                if ( rewriterWithoutSemanticModel.RequiresSemanticModel )
                {
                    // If a semantic model is required, do a second run. We assume this is not likely.
                    var semanticModel = await document.GetSemanticModelAsync( cancellationToken );

                    if ( semanticModel == null )
                    {
                        return document;
                    }

                    var rewriterWithSemanticModel = new CustomSimplifier( semanticModel );
                    newRoot = rewriterWithSemanticModel.Visit( oldRoot )!;
                }

                return document.WithSyntaxRoot( newRoot );
            },
            cancellationToken );

        // Add imports.
        modifiedSolution = await this.TransformAllAsync(
            modifiedSolution,
            documentIds,
            document => ImportAdder.AddImportsAsync( document, Simplifier.Annotation, cancellationToken: cancellationToken ),
            cancellationToken );

        // Run the simplifier.
        modifiedSolution = await this.TransformAllAsync(
            modifiedSolution,
            documentIds,
            async document =>
            {
                var simplifiedDocument = await Simplifier.ReduceAsync( document, Simplifier.Annotation, cancellationToken: cancellationToken );

                if ( simplifiedDocument == document )
                {
                    return document;
                }

                var simplifiedRoot = await simplifiedDocument.GetSyntaxRootAsync( cancellationToken );
                var fixedRoot = new SimplifierFixer().Visit( simplifiedRoot )!;

                return simplifiedDocument.WithSyntaxRoot( fixedRoot );
            },
            cancellationToken );

        // Run the simplifier.
        modifiedSolution = await this.TransformAllAsync(
            modifiedSolution,
            documentIds,
            document => Simplifier.ReduceAsync( document, Simplifier.Annotation, cancellationToken: cancellationToken ),
            cancellationToken );

        // Reformat.
        if ( reformatAll )
        {
            modifiedSolution = await this.TransformAllAsync(
                modifiedSolution,
                documentIds,
                document => Formatter.FormatAsync( document, cancellationToken: cancellationToken ),
                cancellationToken );
        }
        else
        {
            modifiedSolution = await this.TransformAllAsync(
                modifiedSolution,
                documentIds,
                async document =>
                {
                    // Figure out which spans need to be reformatted.
                    var classifiedTextSpans = new ClassifiedTextSpanCollection( await document.GetTextAsync( cancellationToken ) );
                    var visitor = new MarkTextSpansVisitor( classifiedTextSpans );
                    visitor.Visit( await document.GetSyntaxRootAsync( cancellationToken ) );
                    classifiedTextSpans.Polish();
                    var generatedSpans = classifiedTextSpans.Where( s => s.Classification == TextSpanClassification.GeneratedCode ).Select( s => s.Span );

                    // Run the reformatter.
                    return await Formatter.FormatAsync(
                        document,
                        generatedSpans,
                        cancellationToken: cancellationToken );
                },
                cancellationToken );
        }

        return modifiedSolution;
    }

    public Task<PartialCompilation> FormatAsync( IPartialCompilation compilation, CancellationToken cancellationToken = default )
        => this.FormatAsync( (PartialCompilation) compilation, cancellationToken );

    internal async Task<PartialCompilation> FormatAsync( PartialCompilation compilation, CancellationToken cancellationToken = default )
    {
        var (project, syntaxTreeMap) = await WorkspaceHelper.CreateProjectFromCompilationAsync( compilation.Compilation, cancellationToken );

        List<SyntaxTreeTransformation> syntaxTreeReplacements = new( compilation.ModifiedSyntaxTrees.Count );

        var formattedSolution = await this.FormatAsync( project.Solution, syntaxTreeMap.Values, null, false, cancellationToken );

        foreach ( var syntaxTreePair in syntaxTreeMap )
        {
            var oldSyntaxTree = syntaxTreePair.Key;

            var newSyntaxRoot = (await formattedSolution.GetDocument( syntaxTreePair.Value ).AssertNotNull().GetSyntaxRootAsync( cancellationToken ))
                .AssertNotNull();

            var newSyntaxTree = oldSyntaxTree.WithRootAndOptions( newSyntaxRoot, oldSyntaxTree.Options );

            syntaxTreeReplacements.Add( SyntaxTreeTransformation.ReplaceTree( oldSyntaxTree, newSyntaxTree ) );
        }

        return compilation.Update( syntaxTreeReplacements );
    }

    public async Task<Compilation> FormatAllAsync( Compilation compilation, CancellationToken cancellationToken = default )
    {
        var (project, syntaxTreeMap) = await WorkspaceHelper.CreateProjectFromCompilationAsync( compilation, cancellationToken );

        var formattedSolution = await this.FormatAsync( project.Solution, syntaxTreeMap.Values, null, true, cancellationToken );

        return (await formattedSolution.Projects.Single().GetCompilationAsync( cancellationToken )).AssertNotNull();
    }
}