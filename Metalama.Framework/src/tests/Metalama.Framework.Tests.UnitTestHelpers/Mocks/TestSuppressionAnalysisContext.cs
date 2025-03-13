// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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