// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Options;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.DiagnosticAnalysis;

internal interface ISemanticModelAnalysisContext
{
    SemanticModel SemanticModel { get; }

    CancellationToken CancellationToken { get; }

    IProjectOptions ProjectOptions { get; }

    void ReportDiagnostic( Diagnostic diagnostic );
}