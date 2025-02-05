// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Workspaces;

internal sealed class WorkspaceDiagnosticWrapper : IIntrospectionDiagnostic
{
    private readonly WorkspaceDiagnostic _diagnostic;

    public WorkspaceDiagnosticWrapper( WorkspaceDiagnostic diagnostic )
    {
        this._diagnostic = diagnostic;
    }

    public ICompilation Compilation => null!;

    public string Id => "MSBUILD";

    public string Message => this._diagnostic.Message;

    public string? FilePath => null;

    public int? Line => null;

    public IDeclaration? Declaration => null;

    public Severity Severity
        => this._diagnostic.Kind switch
        {
            WorkspaceDiagnosticKind.Failure => Severity.Error,
            WorkspaceDiagnosticKind.Warning => Severity.Warning,
            _ => throw new ArgumentOutOfRangeException()
        };

    public IntrospectionDiagnosticSource Source => IntrospectionDiagnosticSource.MSBuild;

    object? IIntrospectionDiagnostic.Details => null;
}