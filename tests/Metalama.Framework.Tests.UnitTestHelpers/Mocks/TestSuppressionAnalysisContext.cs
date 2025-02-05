// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.DiagnosticSuppressing;
using Metalama.Framework.Engine.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Metalama.Framework.Tests.UnitTestHelpers.Mocks;

public sealed class TestSuppressionAnalysisContext : ISuppressionAnalysisContext
{
    public Compilation Compilation { get; }

    public IProjectOptions ProjectOptions { get; }

    public CancellationToken CancellationToken => CancellationToken.None;

    public ImmutableArray<Diagnostic> ReportedDiagnostics { get; }

    public TestSuppressionAnalysisContext( Compilation compilation, ImmutableArray<Diagnostic> reportedDiagnostics, IProjectOptions projectOptions )
    {
        this.Compilation = compilation;
        this.ProjectOptions = projectOptions;
        this.ReportedDiagnostics = reportedDiagnostics;
    }

    public List<Suppression> ReportedSuppressions { get; } = new();

    public void ReportSuppression( Suppression suppression ) => this.ReportedSuppressions.Add( suppression );
}