// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Introspection;
using Metalama.Framework.Engine.Introspection.References;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Workspaces;

/// <summary>
/// Shows the diagnostics of the reference indexer of a workspace project through <see cref="DiagnosticReporter"/>, which writes
/// them to the console, to the error list of LINQPad, or to the result of a query of the MCP server.
/// </summary>
internal sealed class WorkspaceReferenceDiagnosticSink : IIntrospectionReferenceDiagnosticSink
{
    public void Report( ICompilation compilation, ImmutableArray<Diagnostic> diagnostics )
        => diagnostics.ToIntrospectionDiagnostics( compilation, IntrospectionDiagnosticSource.Metalama ).Report();
}
