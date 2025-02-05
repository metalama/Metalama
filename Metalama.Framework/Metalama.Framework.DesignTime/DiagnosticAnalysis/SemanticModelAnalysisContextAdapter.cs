// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Framework.DesignTime.DiagnosticAnalysis;

internal sealed class SemanticModelAnalysisContextAdapter : ISemanticModelAnalysisContext
{
    private readonly SemanticModelAnalysisContext _context;
    private readonly IProjectOptionsFactory _projectOptionsFactory;

    public SemanticModelAnalysisContextAdapter( SemanticModelAnalysisContext context, IProjectOptionsFactory projectOptionsFactory )
    {
        this._context = context;
        this._projectOptionsFactory = projectOptionsFactory;
    }

    public SemanticModel SemanticModel => this._context.SemanticModel;

    public CancellationToken CancellationToken => this._context.CancellationToken;

    public IProjectOptions ProjectOptions => this._projectOptionsFactory.GetProjectOptions( this._context.Options.AnalyzerConfigOptionsProvider );

    // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
    public void ReportDiagnostic( Diagnostic diagnostic ) => this._context.ReportDiagnostic( diagnostic );
}