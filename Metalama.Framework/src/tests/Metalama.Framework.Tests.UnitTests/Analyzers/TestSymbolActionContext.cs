// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Analyzers;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading;

namespace Metalama.Framework.Tests.UnitTests.Analyzers;

internal sealed class TestSymbolActionContext : ISymbolAnalysisContext
{
    public TestSymbolActionContext( ISymbol symbol, Compilation compilation )
    {
        this.Symbol = symbol;
        this.Compilation = compilation;
    }

    public List<Diagnostic> Diagnostics { get; } = new();

    public Compilation Compilation { get; }

    public ISymbol Symbol { get; }

    public CancellationToken CancellationToken => CancellationToken.None;

    public void ReportDiagnostic( Diagnostic diagnostic ) => this.Diagnostics.Add( diagnostic );
}