// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Formatting;

public abstract partial class FormattedCodeWriter
{
    protected const string CSharpClassTagName = "csharp";
    protected const string DiagnosticTagName = "diagnostic";
    protected const string GeneratingAspectTagName = "aspect";
    private const string _diagnosticAnnotationName = "metalama-diagnostic";
    private readonly ProjectServiceProvider _serviceProvider;

    protected FormattedCodeWriter( in ProjectServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
    }

    public static T AddDiagnosticAnnotations<T>( T syntaxRoot, string? filePath, IEnumerable<Diagnostic>? diagnostics )
        where T : SyntaxNode
    {
        if ( diagnostics == null || filePath == null || syntaxRoot.Span.IsEmpty )
        {
            // Coverage: ignore.
            return syntaxRoot;
        }

        var fileName = Path.GetFileName( filePath );

        var outputSyntaxRoot = syntaxRoot;

        foreach ( var diagnostic in diagnostics )
        {
            var position = diagnostic.Location.GetLineSpan();

            if ( !position.IsValid || !Path.GetFileName( position.Path ).Equals( fileName, StringComparison.OrdinalIgnoreCase ) )
            {
                // The diagnostic is not in the current document.
                continue;
            }

            var serializedDiagnostic = new DiagnosticAnnotation( diagnostic );

            if ( outputSyntaxRoot.Span.Contains( diagnostic.Location.SourceSpan ) )
            {
                var node = outputSyntaxRoot.FindNode( diagnostic.Location.SourceSpan );

                outputSyntaxRoot = outputSyntaxRoot.ReplaceNode(
                    node,
                    node.WithAdditionalAnnotations(
                        new SyntaxAnnotation(
                            _diagnosticAnnotationName,
                            serializedDiagnostic.ToJson() ) ) );
            }
        }

        return outputSyntaxRoot;
    }

    [PublicAPI( "Used from Try" )]
    public static void ProcessAnnotations( ClassifiedTextSpanCollection classifiedTextSpans, SyntaxNode syntaxRoot )
    {
        // Process the annotations by the aspect linker (on the output document).
        FormattingVisitor formattingVisitor = new( classifiedTextSpans );
        formattingVisitor.Visit( syntaxRoot );
    }

    protected async Task<ClassifiedTextSpanCollection> GetClassifiedTextSpansAsync(
        Document document,
        bool areNodesAnnotated = false,
        IEnumerable<Diagnostic>? diagnostics = null,
        bool addTitles = false )
    {
        var sourceText = await document.GetTextAsync();
        var syntaxTree = (await document.GetSyntaxTreeAsync())!;

        if ( await syntaxTree.GetTextAsync() != sourceText )
        {
            throw new AssertionFailedException( "Source text mismatch from Roslyn." );
        }

        var syntaxRoot = await syntaxTree.GetRootAsync();

        var compilation = await document.Project.GetCompilationAsync();
        var semanticModel = compilation!.GetCachedSemanticModel( syntaxTree );
        var classificationService = new ClassificationService( this._serviceProvider );

        ClassifiedTextSpanCollection classifiedTextSpans;

        if ( areNodesAnnotated )
        {
            // Aspect Workbench uses this branch.
            classifiedTextSpans = ClassificationService.GetClassifiedTextSpansOfAnnotatedSyntaxTree( syntaxTree, CancellationToken.None );
        }
        else
        {
            // Annotate the whole syntax tree with the classification service.
            // Note that we don't take into account the output of the template compiler executed from the pipeline,
            // because the template compiler, when executed from the pipeline, only adds annotations to templates, not to the whole syntax tree.

            classifiedTextSpans = classificationService.GetClassifiedTextSpans( semanticModel, false, CancellationToken.None );
        }

        ProcessAnnotations( classifiedTextSpans, syntaxRoot );
        classifiedTextSpans.Polish();

        var classifiedSpans = (await Classifier.GetClassifiedSpansAsync(
                document,
                syntaxRoot.FullSpan ))
            .OrderBy( c => c.TextSpan.Start )
            .ThenBy( c => c.ClassificationType );

        foreach ( var csharpSpan in classifiedSpans )
        {
            var classificationType = csharpSpan.ClassificationType;

            if ( classificationType == "comment" && csharpSpan.TextSpan.Start == 0 )
            {
                classificationType = "header";
            }

            foreach ( var existingSpan in classifiedTextSpans.GetClassifiedSpans( csharpSpan.TextSpan ) )
            {
                var combinedClassification =
                    existingSpan.Tags != null! && existingSpan.Tags.TryGetValue( CSharpClassTagName, out var existingClassification )
                        ? existingClassification + ";" + classificationType
                        : classificationType;

                var intersection = csharpSpan.TextSpan.Intersection( csharpSpan.TextSpan ).AssertNotNull();

                classifiedTextSpans.SetTag( intersection, CSharpClassTagName, combinedClassification );
            }
        }

        // Add XML doc based on the input compilation.
        if ( addTitles )
        {
            var visitor = new AddTitlesVisitor( classifiedTextSpans, semanticModel );
            visitor.Visit( syntaxRoot );
        }

        if ( diagnostics != null && document.FilePath != null )
        {
            AddDiagnostics( document, diagnostics, classifiedTextSpans );
        }

        return classifiedTextSpans;
    }

    private static void AddDiagnostics( Document document, IEnumerable<Diagnostic> diagnostics, ClassifiedTextSpanCollection classifiedTextSpans )
    {
        foreach ( var diagnostic in diagnostics )
        {
            var position = diagnostic.Location.GetLineSpan();

            if ( !position.IsValid || !position.Path.EndsWith( document.FilePath!, StringComparison.OrdinalIgnoreCase ) )
            {
                // The diagnostic is not in the current document.
                continue;
            }

            foreach ( var span in classifiedTextSpans.GetClassifiedSpans( diagnostic.Location.SourceSpan ) )
            {
                if ( diagnostic.Location.SourceSpan.IntersectsWith( span.Span ) )
                {
                    classifiedTextSpans.SetTag(
                        diagnostic.Location.SourceSpan,
                        DiagnosticTagName,
                        new DiagnosticAnnotation( diagnostic ).ToJson() );
                }
            }
        }
    }
}