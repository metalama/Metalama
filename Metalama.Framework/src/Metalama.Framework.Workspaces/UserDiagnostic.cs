// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Introspection;

namespace Metalama.Framework.Workspaces;

internal sealed class UserDiagnostic : IIntrospectionDiagnostic
{
    public UserDiagnostic( Severity severity, string id, string message, string? filePath, int? line, IDeclaration? declaration, object? details )
    {
        this.Id = id;
        this.Message = message;
        this.FilePath = filePath;
        this.Line = line;
        this.Declaration = declaration;
        this.Severity = severity;
        this.Details = details;
    }

    public ICompilation? Compilation => this.Declaration?.Compilation;

    public string Id { get; }

    public string Message { get; }

    public string? FilePath { get; }

    public int? Line { get; }

    public IDeclaration? Declaration { get; }

    public Severity Severity { get; }

    public IntrospectionDiagnosticSource Source => IntrospectionDiagnosticSource.User;

    public object? Details { get; }
}