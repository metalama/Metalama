// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Analyzers;

[DiagnosticAnalyzer( LanguageNames.CSharp )]
[UsedImplicitly]
public partial class MetalamaInternalsAnalyzer : DiagnosticAnalyzer
{
    // Range: 820-829
    private static readonly DiagnosticDescriptor _cannotConsumeApi = new(
        "LAMA0820",
        "Cannot reference an internal Metalama API.",
        "'{0}' is an internal Metalama API and should not be referenced in user code.",
        "Metalama",
        DiagnosticSeverity.Warning,
        true );

    private static readonly DiagnosticDescriptor _cannotExposeApi = new(
        "LAMA0821",
        "Cannot expose an internal Metalama API.",
        "Cannot expose the internal Metalama API '{0}'.",
        "Metalama",
        DiagnosticSeverity.Warning,
        true );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _cannotConsumeApi, _cannotExposeApi );

    public override void Initialize( AnalysisContext context )
    {
        context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
        context.EnableConcurrentExecution();
        context.RegisterSemanticModelAction( AnalyzeSemanticModel );
    }

    private static void AnalyzeSemanticModel( SemanticModelAnalysisContext context )
    {
        var projectKind = ProjectClassifier.GetProjectKind( context.SemanticModel.Compilation.AssemblyName );

        switch ( projectKind )
        {
            case ProjectKind.MetalamaPublicApi:
                new PublicApiVisitor( context ).Visit( context.SemanticModel.SyntaxTree.GetRoot() );

                break;

            case ProjectKind.UserProject:
                new UserProjectVisitor( context ).Visit( context.SemanticModel.SyntaxTree.GetRoot() );

                break;
        }
    }
}