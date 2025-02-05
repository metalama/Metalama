// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.DiagnosticSuppressing;

internal sealed class SuppressionAnalysisContextAdapter : ISuppressionAnalysisContext
{
    private readonly SuppressionAnalysisContext _context;
    private readonly IProjectOptionsFactory _projectOptionsFactory;

    public SuppressionAnalysisContextAdapter( SuppressionAnalysisContext context, IProjectOptionsFactory projectOptionsFactory )
    {
        this._context = context;
        this._projectOptionsFactory = projectOptionsFactory;
    }

    public Compilation Compilation => this._context.Compilation;

    public IProjectOptions ProjectOptions => this._projectOptionsFactory.GetProjectOptions( this._context.Options.AnalyzerConfigOptionsProvider );

    public CancellationToken CancellationToken => this._context.CancellationToken;

    public ImmutableArray<Diagnostic> ReportedDiagnostics => this._context.ReportedDiagnostics;

    // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
    public void ReportSuppression( Suppression suppression ) => this._context.ReportSuppression( suppression );
}