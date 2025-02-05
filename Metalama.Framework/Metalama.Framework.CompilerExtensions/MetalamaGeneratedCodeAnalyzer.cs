// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Metalama.Framework.CompilerExtensions;

#pragma warning disable RS1026 // Enable concurrent execution
#pragma warning disable RS1025 // Configure generated code analysis

// Analyzer that produces diagnostics when aspects are used in generated code (including e.g. through fabrics).
[DiagnosticAnalyzer( LanguageNames.CSharp )]
public sealed class MetalamaGeneratedCodeAnalyzer : DiagnosticAnalyzer
{
    private readonly DiagnosticAnalyzer? _impl;

    public MetalamaGeneratedCodeAnalyzer()
    {
        this._impl = (DiagnosticAnalyzer) ResourceExtractor.CreateInstance( "Metalama.Framework.Engine", "Metalama.Framework.Engine.GeneratedCodeAnalyzer" );
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => this._impl?.SupportedDiagnostics ?? ImmutableArray<DiagnosticDescriptor>.Empty;

    public override void Initialize( AnalysisContext context ) => this._impl?.Initialize( context );
}