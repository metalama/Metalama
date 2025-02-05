// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.DiagnosticSuppressing;

internal interface ISuppressionAnalysisContext
{
    Compilation Compilation { get; }

    IProjectOptions ProjectOptions { get; }

    CancellationToken CancellationToken { get; }

    ImmutableArray<Diagnostic> ReportedDiagnostics { get; }

    void ReportSuppression( Suppression suppression );
}