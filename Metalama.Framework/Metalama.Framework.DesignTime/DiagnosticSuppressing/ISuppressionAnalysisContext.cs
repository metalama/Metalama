// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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