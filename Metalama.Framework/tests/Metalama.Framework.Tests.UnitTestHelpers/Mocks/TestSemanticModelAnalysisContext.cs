// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.DiagnosticAnalysis;
using Metalama.Framework.Engine.Options;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading;

namespace Metalama.Framework.Tests.UnitTestHelpers.Mocks;

public sealed class TestSemanticModelAnalysisContext : ISemanticModelAnalysisContext
{
    public TestSemanticModelAnalysisContext( SemanticModel semanticModel, IProjectOptions projectOptions )
    {
        this.SemanticModel = semanticModel;
        this.ProjectOptions = projectOptions;
    }

    public SemanticModel SemanticModel { get; }

    public CancellationToken CancellationToken => default;

    public IProjectOptions ProjectOptions { get; }

    public void ReportDiagnostic( Diagnostic diagnostic ) => this.ReportedDiagnostics.Add( diagnostic );

    public List<Diagnostic> ReportedDiagnostics { get; } = new();
}