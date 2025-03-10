// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.SourceTransformer;

[DiagnosticAnalyzer( "CSharp" )]
[UsedImplicitly]
internal class LinqAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor _warning =
        new(
            "CMT005",
            "Do not use Enumerable.ToList",
            "Use ToReadOnlyList or ToMutableList",
            "Metalama.SourceTransformer",
            DiagnosticSeverity.Error,
            true );

    public override void Initialize( AnalysisContext context )
    {
        context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
        context.EnableConcurrentExecution();
        context.RegisterSemanticModelAction( AnalyzeSemanticModel );
    }

    private static void AnalyzeSemanticModel( SemanticModelAnalysisContext context )
    {
        var visitor = new Visitor( context.SemanticModel, context.ReportDiagnostic );
        visitor.Visit( context.SemanticModel.SyntaxTree.GetRoot() );
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _warning );

    private sealed class Visitor : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly Action<Diagnostic> _reportDiagnostic;

        public Visitor( SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node ) : base( depth )
        {
            this._semanticModel = semanticModel;
            this._reportDiagnostic = reportDiagnostic;
        }

        public override void VisitIdentifierName( IdentifierNameSyntax node )
        {
            if ( node.Identifier.Text == nameof(Enumerable.ToList) )
            {
                if ( this._semanticModel.GetSymbolInfo( node ).Symbol is IMethodSymbol symbol && symbol.ContainingType.Name == nameof(Enumerable) )
                {
                    this._reportDiagnostic( Diagnostic.Create( _warning, node.Identifier.GetLocation() ) );
                }
            }
        }
    }
}