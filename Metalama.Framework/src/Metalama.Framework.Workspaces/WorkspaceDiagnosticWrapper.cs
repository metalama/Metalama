// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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