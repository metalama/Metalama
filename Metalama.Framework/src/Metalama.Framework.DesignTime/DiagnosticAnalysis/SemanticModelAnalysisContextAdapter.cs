// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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