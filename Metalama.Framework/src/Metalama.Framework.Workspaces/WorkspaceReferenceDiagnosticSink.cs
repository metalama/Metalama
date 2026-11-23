// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Introspection;
using Metalama.Framework.Engine.Introspection.References;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Workspaces;

/// <summary>
/// Shows the diagnostics of the reference indexer of a workspace project through <see cref="DiagnosticReporter"/>, which writes
/// them to the console, to the error list of LINQPad, or to the result of a query of the MCP server.
/// </summary>
/// <remarks>
/// The reference graph reports the diagnostics of an incomplete index for every query, so this class reports each diagnostic at
/// most once per scope of <see cref="DiagnosticReporter"/>. A host starts a new scope with <see cref="DiagnosticReporter.ClearCounters"/>
/// at the start of each request, so that each response that depends on the index lists the diagnostics once.
/// </remarks>
internal sealed class WorkspaceReferenceDiagnosticSink : IIntrospectionReferenceDiagnosticSink
{
    private readonly object _sync = new();
    private readonly HashSet<Diagnostic> _reportedDiagnostics = new();
    private int _scope = -1;

    public void Report( ICompilation compilation, ImmutableArray<Diagnostic> diagnostics )
    {
        ImmutableArray<Diagnostic> newDiagnostics;

        lock ( this._sync )
        {
            var scope = DiagnosticReporter.Scope;

            if ( scope != this._scope )
            {
                this._reportedDiagnostics.Clear();
                this._scope = scope;
            }

            newDiagnostics = diagnostics.Where( this._reportedDiagnostics.Add ).ToImmutableArray();
        }

        if ( !newDiagnostics.IsEmpty )
        {
            newDiagnostics.ToIntrospectionDiagnostics( compilation, IntrospectionDiagnosticSource.Metalama ).Report();
        }
    }
}
