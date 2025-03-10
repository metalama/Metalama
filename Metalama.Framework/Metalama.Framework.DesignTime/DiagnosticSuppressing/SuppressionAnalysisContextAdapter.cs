// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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