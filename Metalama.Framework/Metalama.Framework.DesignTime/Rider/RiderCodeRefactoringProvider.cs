// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.CodeFixes;
using Metalama.Framework.DesignTime.DiagnosticAnalysis;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Rider;

[UsedImplicitly]
public sealed class RiderCodeRefactoringProvider : TheCodeRefactoringProvider
{
    private readonly TheCodeFixProvider _codeFixProvider;
    private readonly TheDiagnosticAnalyzer _diagnosticAnalyzer;

    public RiderCodeRefactoringProvider() : this( DesignTimeServiceProviderFactory.GetSharedServiceProvider() ) { }

    public RiderCodeRefactoringProvider( GlobalServiceProvider serviceProvider ) : base( serviceProvider )
    {
        this._codeFixProvider = new TheCodeFixProvider( serviceProvider );
        this._diagnosticAnalyzer = new TheDiagnosticAnalyzer( serviceProvider );
    }

    public override async Task ComputeRefactoringsAsync( ICodeRefactoringContext context )
    {
        // Report regular refactorings.
        await base.ComputeRefactoringsAsync( context );

        // Compute hidden diagnostics for the given span.
        var semanticModel = await context.Document.GetSemanticModelAsync( context.CancellationToken );

        if ( semanticModel == null )
        {
            return;
        }

        var compilationWithAnalyzer = semanticModel.Compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>( this._diagnosticAnalyzer ),
            new CompilationWithAnalyzersOptions(
                context.Document.Project.AnalyzerOptions,
                onAnalyzerException: null,
                concurrentAnalysis: true,
                logAnalyzerExecutionTime: false ) );

        var syntaxRoot = await semanticModel.SyntaxTree.GetRootAsync();

        if ( !syntaxRoot.Span.Contains( context.Span ) )
        {
            return;
        }

        var diagnostics =
            await compilationWithAnalyzer.GetAnalyzerSemanticDiagnosticsAsync( semanticModel, filterSpan: context.Span, context.CancellationToken );

        var filteredDiagnostics =
            diagnostics.Where( d => d.Severity == DiagnosticSeverity.Hidden && d.Location.SourceSpan.IntersectsWith( context.Span ) ).ToImmutableArray();

        // Report code fixes for found diagnostics as refactorings.
        await this._codeFixProvider.RegisterCodeFixesAsync( new HiddenCodeFixToCodeRefactoringContext( context, filteredDiagnostics ) );
    }

    private sealed class HiddenCodeFixToCodeRefactoringContext : ICodeFixContext
    {
        private readonly ICodeRefactoringContext _codeRefactoringContext;

        public HiddenCodeFixToCodeRefactoringContext( ICodeRefactoringContext codeRefactoringContext, ImmutableArray<Diagnostic> diagnostics )
        {
            this._codeRefactoringContext = codeRefactoringContext;
            this.Diagnostics = diagnostics;
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; }

        public Document Document => this._codeRefactoringContext.Document;

        public TextSpan Span => this._codeRefactoringContext.Span;

        public CancellationToken CancellationToken => this._codeRefactoringContext.CancellationToken;

        public void RegisterCodeFix( CodeAction action, ImmutableArray<Diagnostic> diagnostics ) => this._codeRefactoringContext.RegisterRefactoring( action );
    }
}