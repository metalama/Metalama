// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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