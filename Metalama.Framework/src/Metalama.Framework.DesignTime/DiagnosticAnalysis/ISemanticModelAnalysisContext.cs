// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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